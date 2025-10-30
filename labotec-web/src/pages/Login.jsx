import React, { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { login } from '../lib/api'

export default function Login(){
  const [userName, setUser] = useState('admin')
  const [password, setPass] = useState('Admin#2025!')
  const [err, setErr] = useState('')
  const nav = useNavigate()

  const submit = async (e)=>{
    e.preventDefault()
    try{
      const { token } = await login(userName, password)
      localStorage.setItem('token', token)
      nav('/patients')
    }catch(e){ setErr(e.message) }
  }

  return (
    <form onSubmit={submit} style={{maxWidth:360, margin:'40px auto', display:'grid', gap:12}}>
      <h3>Entrar</h3>
      {err && <div style={{color:'crimson'}}>{err}</div>}
      <input placeholder="Usuario" value={userName} onChange={e=>setUser(e.target.value)} />
      <input placeholder="Contraseña" type="password" value={password} onChange={e=>setPass(e.target.value)} />
      <button>Entrar</button>
    </form>
  )
}
