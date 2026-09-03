import { api } from './apiClient.js';

export function getDashboard() {
    return api('/dashboard');
}
