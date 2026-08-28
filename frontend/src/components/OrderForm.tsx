import { useState } from 'react';
import type { FormEvent } from 'react';
import { ApiError, rxflowApi } from '../lib/apiClient';
import type { CreateOrderRequest, SubmitOrderResult } from '../lib/types';

interface OrderFormProps {
  token: string;
  onSubmitted: (result: SubmitOrderResult) => void;
}

interface FormState {
  sphere: string;
  cylinder: string;
  axis: string;
  frameId: string;
  frameA: string;
  frameB: string;
}

const initialState: FormState = {
  sphere: '1',
  cylinder: '0',
  axis: '90',
  frameId: 'F-001',
  frameA: '50',
  frameB: '40',
};

const numericFields: Array<{ key: keyof FormState; label: string }> = [
  { key: 'sphere', label: 'Sphere' },
  { key: 'cylinder', label: 'Cylinder' },
  { key: 'axis', label: 'Axis' },
  { key: 'frameA', label: 'Frame A' },
  { key: 'frameB', label: 'Frame B' },
];

/**
 * This component checks only that required fields are present and that numeric fields actually
 * parse as numbers — transport-level, not domain, validation. Everything else (grindability,
 * pricing, frame dimension rules) is decided by the backend; this form just relays whatever error
 * message it returns. See the frontend/backend boundary rule in Agents.md.
 */
function validate(form: FormState): Record<string, string> {
  const errors: Record<string, string> = {};
  if (!form.frameId.trim()) errors.frameId = 'Frame ID is required.';
  for (const { key, label } of numericFields) {
    if (form[key].trim() === '' || Number.isNaN(Number(form[key]))) {
      errors[key] = `${label} must be a number.`;
    }
  }
  return errors;
}

export function OrderForm({ token, onSubmitted }: OrderFormProps) {
  const [form, setForm] = useState<FormState>(initialState);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [submitError, setSubmitError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  function update<K extends keyof FormState>(key: K, value: string) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    const errors = validate(form);
    setFieldErrors(errors);
    setSubmitError('');
    if (Object.keys(errors).length > 0) return;

    const request: CreateOrderRequest = {
      sphere: Number(form.sphere),
      cylinder: Number(form.cylinder),
      axis: Number(form.axis),
      frameId: form.frameId.trim(),
      frameA: Number(form.frameA),
      frameB: Number(form.frameB),
    };

    setSubmitting(true);
    try {
      const result = await rxflowApi.submitOrder(request, token);
      onSubmitted(result);
    } catch (error) {
      setSubmitError(error instanceof ApiError ? error.message : 'Order submission failed unexpectedly.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form className="order-form" onSubmit={handleSubmit} noValidate>
      <h2>Submit a prescription order</h2>
      <label>
        Sphere
        <input value={form.sphere} onChange={(e) => update('sphere', e.target.value)} inputMode="decimal" />
        {fieldErrors.sphere && <span className="field-error">{fieldErrors.sphere}</span>}
      </label>
      <label>
        Cylinder
        <input value={form.cylinder} onChange={(e) => update('cylinder', e.target.value)} inputMode="decimal" />
        {fieldErrors.cylinder && <span className="field-error">{fieldErrors.cylinder}</span>}
      </label>
      <label>
        Axis
        <input value={form.axis} onChange={(e) => update('axis', e.target.value)} inputMode="numeric" />
        {fieldErrors.axis && <span className="field-error">{fieldErrors.axis}</span>}
      </label>
      <label>
        Frame ID
        <input value={form.frameId} onChange={(e) => update('frameId', e.target.value)} />
        {fieldErrors.frameId && <span className="field-error">{fieldErrors.frameId}</span>}
      </label>
      <label>
        Frame A
        <input value={form.frameA} onChange={(e) => update('frameA', e.target.value)} inputMode="decimal" />
        {fieldErrors.frameA && <span className="field-error">{fieldErrors.frameA}</span>}
      </label>
      <label>
        Frame B
        <input value={form.frameB} onChange={(e) => update('frameB', e.target.value)} inputMode="decimal" />
        {fieldErrors.frameB && <span className="field-error">{fieldErrors.frameB}</span>}
      </label>
      <button type="submit" disabled={submitting}>
        {submitting ? 'Submitting…' : 'Submit order'}
      </button>
      {submitError && <p role="alert">{submitError}</p>}
    </form>
  );
}
