import type { ApiErrorBody, CreateOrderRequest, OrderStatusView, SubmitOrderResult } from './types';

// Falls back to the port RxFlow.Api listens on both locally (dotnet run, see
// src/RxFlow.Api/Properties/launchSettings.json) and under Compose (deploy/docker-compose.yml
// maps the container's 8080 to host 5080) — see frontend/.env.example to override.
const DEFAULT_BASE_URL = 'http://localhost:5080';

function baseUrl(): string {
  return import.meta.env.VITE_API_BASE_URL ?? DEFAULT_BASE_URL;
}

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

/** Thrown specifically for a route the backend does not implement yet (see LabOverridePanel). */
export class NotImplementedError extends ApiError {
  constructor(status: number) {
    super(status, 'This backend route is not implemented yet.');
    this.name = 'NotImplementedError';
  }
}

async function readErrorMessage(response: Response): Promise<string> {
  try {
    const body = (await response.json()) as ApiErrorBody;
    if (body?.error) return body.error;
  } catch {
    // response had no JSON body — fall through to the status-based message
  }
  return `Request failed with status ${response.status}`;
}

async function request<T>(path: string, token: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${baseUrl()}${path}`, {
    ...init,
    headers: {
      'content-type': 'application/json',
      ...(token ? { authorization: `Bearer ${token}` } : {}),
      ...init?.headers,
    },
  });

  if (!response.ok) {
    throw new ApiError(response.status, await readErrorMessage(response));
  }
  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

export const rxflowApi = {
  submitOrder(order: CreateOrderRequest, token: string): Promise<SubmitOrderResult> {
    return request<SubmitOrderResult>('/orders', token, {
      method: 'POST',
      body: JSON.stringify(order),
    });
  },

  getOrderStatus(orderId: string, token: string): Promise<OrderStatusView> {
    return request<OrderStatusView>(`/reports/orders/${orderId}`, token);
  },

  async requestLabOverride(orderId: string, reason: string, token: string): Promise<void> {
    try {
      await request<void>(`/orders/${orderId}/lab-override`, token, {
        method: 'POST',
        body: JSON.stringify({ reason }),
      });
    } catch (error) {
      // No route currently implements lab-override (open item — see Requirements.md "open
      // questions" and architecture.md's API/integration-surface diagram); every response for this
      // path is either "route not found" or "method not allowed", never a real business outcome.
      if (error instanceof ApiError && (error.status === 404 || error.status === 405)) {
        throw new NotImplementedError(error.status);
      }
      throw error;
    }
  },
};
