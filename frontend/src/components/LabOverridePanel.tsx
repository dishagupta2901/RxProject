import { useState } from 'react';
import type { FormEvent } from 'react';
import { NotImplementedError, rxflowApi } from '../lib/apiClient';
import { decodeJwtRoles } from '../lib/jwt';

interface LabOverridePanelProps {
  token: string;
  orderId: string;
}

type Outcome = { kind: 'success' } | { kind: 'not-implemented' } | { kind: 'error'; message: string };

/**
 * Entry point for the lab-override workflow. There is no backend workflow behind this yet — the
 * `LabOverride` authorization policy exists in Program.cs but no endpoint requires it (see
 * Requirements.md "open questions" and architecture.md's API/integration-surface diagram). This
 * panel is real: it sends a real request to the documented future route and shows whatever the
 * backend actually returns, including "not implemented" — it does not fake a working override.
 */
export function LabOverridePanel({ token, orderId }: LabOverridePanelProps) {
  const [reason, setReason] = useState('');
  const [outcome, setOutcome] = useState<Outcome | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const hasLabOverrideRole = decodeJwtRoles(token).includes('lab-override');

  async function submitOverride(event: FormEvent) {
    event.preventDefault();
    if (!orderId.trim() || !reason.trim()) {
      setOutcome({ kind: 'error', message: 'An order ID and a reason are both required.' });
      return;
    }
    setSubmitting(true);
    setOutcome(null);
    try {
      await rxflowApi.requestLabOverride(orderId.trim(), reason.trim(), token);
      setOutcome({ kind: 'success' });
    } catch (error) {
      setOutcome(
        error instanceof NotImplementedError
          ? { kind: 'not-implemented' }
          : { kind: 'error', message: error instanceof Error ? error.message : 'Override request failed.' },
      );
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <section className="lab-override-panel">
      <h2>Lab override</h2>
      {!hasLabOverrideRole && (
        <p className="hint">
          Your bearer token does not carry the <code>lab-override</code> role — the backend will reject
          this request. Shown anyway so you can see the real response.
        </p>
      )}
      <form onSubmit={submitOverride}>
        <label>
          Reason
          <textarea value={reason} onChange={(e) => setReason(e.target.value)} rows={2} />
        </label>
        <button type="submit" disabled={submitting}>
          {submitting ? 'Sending…' : 'Request lab override'}
        </button>
      </form>
      {outcome?.kind === 'success' && <p role="status">Override accepted.</p>}
      {outcome?.kind === 'not-implemented' && (
        <p role="status" className="hint">
          The backend does not implement a lab-override endpoint yet — this is a known, documented gap
          (see Requirements.md), not a bug in this panel.
        </p>
      )}
      {outcome?.kind === 'error' && <p role="alert">{outcome.message}</p>}
    </section>
  );
}
