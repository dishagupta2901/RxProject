// Mirrors the backend contract as configured today (System.Text.Json with a JsonStringEnumConverter,
// see src/RxFlow.Api/Program.cs). Keep in sync with:
//   - RxFlow.Api.OrdersController (CreateOrderRequest / SubmitOrderResult)
//   - RxFlow.Reporting.OrderStatusView
//   - RxFlow.Domain.OrderStatus
// There is no generated-client step yet (open item in DECISIONS.md); this file is the hand-maintained
// contract boundary until one is selected.

export type OrderStatus = 'Submitted' | 'Validated' | 'Routed' | 'Scheduled' | 'Shipped' | 'Rejected';

export interface CreateOrderRequest {
  sphere: number;
  cylinder: number;
  axis: number;
  frameId: string;
  frameA: number;
  frameB: number;
}

export interface SubmitOrderResult {
  orderId: string;
  price: number;
  status: OrderStatus;
}

export interface OrderStatusView {
  orderId: string;
  status: OrderStatus;
  frameId: string;
  sphere: number;
  cylinder: number;
  axis: number;
}

export interface ApiErrorBody {
  error?: string;
}
