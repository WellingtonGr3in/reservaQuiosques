import { useCallback, useEffect, useState } from 'react';
import { ApiError } from '../api/client';

interface State<T> {
  data: T | null;
  error: ApiError | null;
  loading: boolean;
}

// Carrega dados da API quando o componente aparece (e de novo quando "deps" muda).
export function useApi<T>(load: () => Promise<T>, deps: unknown[]) {
  const [state, setState] = useState<State<T>>({ data: null, error: null, loading: true });
  const [version, setVersion] = useState(0);

  useEffect(() => {
    let active = true;
    setState((s) => ({ ...s, loading: true, error: null }));
    load()
      .then((data) => active && setState({ data, error: null, loading: false }))
      .catch((error) =>
        active && setState({ data: null, error: error instanceof ApiError ? error : new ApiError(0, String(error)), loading: false }),
      );
    return () => {
      active = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...deps, version]);

  const reload = useCallback(() => setVersion((v) => v + 1), []);
  const setData = useCallback((data: T) => setState({ data, error: null, loading: false }), []);

  return { ...state, reload, setData };
}
