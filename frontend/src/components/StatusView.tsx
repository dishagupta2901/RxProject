import { useState } from 'react';
import type { FormEvent } from 'react';
import { ApiError, rxflowApi } from '../lib/apiClient';
import type { OrderStatusView } from '../lib/types';

interface StatusViewProps {
  token: string;
  orderId: string;
  onOrderIdChange: (orderId: string) => void;
}

export function StatusView({ token, orderId, onOrderIdChange }: StatusViewProps) {
  const [view, setView] = useState<OrderStatusView | null>(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  async function lookup(event: FormEvent) {
    event.preventDefault();
    if (!orderId.trim()) {
      setError('Enter an order ID to look up.');
      return;
    }
    setLoading(true);
    setError('');
    setView(null);
    try {
      setView(await rxflowApi.getOrderStatus(orderId.trim(), token));
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        setError(`No order found with ID ${orderId.trim()}.`);
      } else {
        setError(err instanceof ApiError ? err.message : 'Status lookup failed unexpectedly.');
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <section className="status-view">
      <h2>Trace an order</h2>
      <form onSubmit={lookup}>
        <label>
          Order ID
          <input value={orderId} onChange={(e) => onOrderIdChange(e.target.value)} placeholder="order GUID" />
        </label>
        <button type="submit" disabled={loading}>
          {loading ? 'Looking up…' : 'Look up status'}
        </button>
      </form>
      {error && <p role="alert">{error}</p>}
      {view && (
        <dl className="order-status">
          <dt>Order ID</dt>
          <dd>{view.orderId}</dd>
          <dt>Status</dt>
          <dd>
            <span className={`status-badge status-${view.status.toLowerCase()}`}>{view.status}</span>
          </dd>
          <dt>Frame</dt>
          <dd>{view.frameId}</dd>
          <dt>Prescription</dt>
          <dd>
            sphere {view.sphere}, cylinder {view.cylinder}, axis {view.axis}
          </dd>
        </dl>
      )}
    </section>
  );
}
