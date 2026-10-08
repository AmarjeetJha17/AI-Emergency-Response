import http from 'k6/http';
import { check, sleep } from 'k6';

const baseUrl = __ENV.BASE_URL || 'http://localhost:5000';
const token = __ENV.BEARER_TOKEN || '';

export const options = {
  vus: Number(__ENV.VUS || 10),
  duration: __ENV.DURATION || '2m',
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<1000'],
  },
};

export default function () {
  const headers = token ? { Authorization: `Bearer ${token}` } : {};
  const incidents = http.get(`${baseUrl}/api/incidents?page=1&pageSize=20`, { headers });
  check(incidents, { 'incident retrieval responds successfully': (r) => r.status === 200 });

  const dashboard = http.get(`${baseUrl}/api/dashboard`, { headers });
  check(dashboard, { 'dashboard request responds': (r) => r.status < 500 });
  sleep(1);
}
