import { useState } from 'react';
import { LabOverridePanel } from './components/LabOverridePanel';
import { OrderForm } from './components/OrderForm';
import { StatusView } from './components/StatusView';
import { TokenField } from './components/TokenField';
import type { SubmitOrderResult } from './lib/types';

export function App() {
  const [token, setToken] = useState('');
  const [lastSubmitted, setLastSubmitted] = useState<SubmitOrderResult | null>(null);
  const [orderId, setOrderId] = useState('');

  function handleSubmitted(result: SubmitOrderResult) {
    setLastSubmitted(result);
    setOrderId(result.orderId);
  }

  return (
    <main className="app">
      <header>
        <h1>RxFlow Order Desk</h1>
        <p className="subtitle">Optician client for the RxFlow training lab — talks only to the RxFlow API.</p>
      </header>

      <TokenField token={token} onChange={setToken} />

      <div className="panels">
        <OrderForm token={token} onSubmitted={handleSubmitted} />
        <StatusView token={token} orderId={orderId} onOrderIdChange={setOrderId} />
        <LabOverridePanel token={token} orderId={orderId} />
      </div>

      {lastSubmitted && (
        <section className="last-result" aria-label="Last submission result">
          <h2>Last submission</h2>
          <pre>{JSON.stringify(lastSubmitted, null, 2)}</pre>
        </section>
      )}
    </main>
  );
}
