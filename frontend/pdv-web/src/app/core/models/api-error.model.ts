export interface ApiValidationProblem {
  title?: string;
  detail?: string;
  message?: string;
  errors?: Record<string, string[]>;
}
