import { afterEach, describe, expect, it, vi } from 'vitest';
import { ApiError, NotImplementedError, rxflowApi } from './apiClient';

function jsonResponse(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/json' } });
}

describe('rxflowApi', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('submits an order and returns the parsed result', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      jsonResponse(202, { orderId: 'order-1', price: 100, status: 'Submitted' }),
    );
    vi.stubGlobal('fetch', fetchMock);

    const result = await rxflowApi.submitOrder(
      { sphere: 1, cylinder: 0, axis: 90, frameId: 'F-001', frameA: 50, frameB: 40 },
      'a-token',
    );

    expect(result).toEqual({ orderId: 'order-1', price: 100, status: 'Submitted' });
    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe('http://localhost:5080/orders');
    expect(init.method).toBe('POST');
    expect(init.headers.authorization).toBe('Bearer a-token');
  });

  it('throws ApiError with the backend message on a validation failure', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(jsonResponse(400, { error: 'Axis must be between 0 and 180.' })));

    const error = await rxflowApi
      .submitOrder({ sphere: 1, cylinder: 0, axis: 999, frameId: 'F-001', frameA: 50, frameB: 40 }, '')
      .catch((e) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect(error.status).toBe(400);
    expect(error.message).toBe('Axis must be between 0 and 180.');
  });

  it('reports order-not-found as a plain 404 ApiError, not NotImplementedError', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(jsonResponse(404, {})));

    const error = await rxflowApi.getOrderStatus('missing-id', 'a-token').catch((e) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect(error).not.toBeInstanceOf(NotImplementedError);
    expect(error.status).toBe(404);
  });

  it('reports lab-override as NotImplementedError when the route does not exist', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 404 })));

    const error = await rxflowApi.requestLabOverride('order-1', 'reason', 'a-token').catch((e) => e);

    expect(error).toBeInstanceOf(NotImplementedError);
  });
});
