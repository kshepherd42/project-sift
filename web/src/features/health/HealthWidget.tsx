import { useCallback, useEffect, useState } from 'react';
import { StatusPanel } from '../../components/StatusPanel';
import { ApiError } from '../../gateway/apiClient';
import { fetchHealth, type HealthResponse } from '../../gateway/healthGateway';

type HealthState = {
  loading: boolean;
  statusCode: number | null;
  payload: HealthResponse | null;
  errorMessage: string | null;
};

const initialState: HealthState = {
  loading: true,
  statusCode: null,
  payload: null,
  errorMessage: null,
};

export function HealthWidget() {
  const [state, setState] = useState<HealthState>(initialState);

  const loadHealth = useCallback(async () => {
    setState({ loading: true, statusCode: null, payload: null, errorMessage: null });

    try {
      const result = await fetchHealth();
      setState({
        loading: false,
        statusCode: result.statusCode,
        payload: result.data,
        errorMessage: null,
      });
    } catch (error) {
      if (error instanceof ApiError) {
        setState({
          loading: false,
          statusCode: error.statusCode,
          payload: null,
          errorMessage: error.message,
        });
        return;
      }

      setState({
        loading: false,
        statusCode: null,
        payload: null,
        errorMessage: 'Unable to reach the API. Check that the backend is running and try again.',
      });
    }
  }, []);

  useEffect(() => {
    void loadHealth();
  }, [loadHealth]);

  return (
    <StatusPanel title="Health Check">
      {state.loading && <p role="status">Loading API health...</p>}

      {!state.loading && state.errorMessage && (
        <div>
          <p role="alert">{state.errorMessage}</p>
          {state.statusCode !== null && <p>HTTP Status: {state.statusCode}</p>}
          <button type="button" onClick={() => void loadHealth()}>
            Retry
          </button>
        </div>
      )}

      {!state.loading && !state.errorMessage && state.payload && (
        <div>
          <p>HTTP Status: {state.statusCode}</p>
          <pre aria-label="health-json">{JSON.stringify(state.payload, null, 2)}</pre>
          <button type="button" onClick={() => void loadHealth()}>
            Retry
          </button>
        </div>
      )}
    </StatusPanel>
  );
}
