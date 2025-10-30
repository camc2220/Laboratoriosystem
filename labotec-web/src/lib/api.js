const base = import.meta.env.VITE_API_BASE || 'http://localhost:8080'
function headers(){
  const token = localStorage.getItem('token')
  return token ? { 'Authorization': 'Bearer '+token } : {}
}
export async function login(userName, password){
  const res = await fetch(base+'/api/auth/login', { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ userName, password }) })
  if(!res.ok) throw new Error('Credenciales inválidas')
  return res.json()
}
export async function getPaged(url){ const res = await fetch(base+url, { headers: headers() }); if(!res.ok) throw new Error('Error'); return res.json() }
export async function postForm(url, form){ const res = await fetch(base+url, { method:'POST', headers: headers(), body: form }); if(!res.ok) throw new Error('Error'); return res.json() }
