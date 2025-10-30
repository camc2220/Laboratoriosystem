import React from 'react'
import { Routes, Route, Link, Navigate, useNavigate } from 'react-router-dom'
import Login from './pages/Login.jsx'
import Patients from './pages/Patients.jsx'
import Results from './pages/Results.jsx'
import Invoices from './pages/Invoices.jsx'

function useAuth(){
  const token = localStorage.getItem('token')
  return { token, isAuth: !!token }
}

function Protected({ children }){
  const { isAuth } = useAuth()
  return isAuth ? children : <Navigate to="/login" replace />
}

export default function App(){
  const nav = useNavigate()
  const logout = () => { localStorage.removeItem('token'); nav('/login') }
  return (
    <div style={{fontFamily:'system-ui', padding:'16px'}}>
      <header style={{display:'flex', gap:16, alignItems:'center'}}>
        <h3>LABOTEC</h3>
        <Link to="/patients">Pacientes</Link>
        <Link to="/results">Resultados</Link>
        <Link to="/invoices">Facturas</Link>
        <div style={{marginLeft:'auto'}}>
          <button onClick={logout}>Salir</button>
        </div>
      </header>
      <Routes>
        <Route path="/login" element={<Login/>}/>
        <Route path="/patients" element={<Protected><Patients/></Protected>}/>
        <Route path="/results" element={<Protected><Results/></Protected>}/>
        <Route path="/invoices" element={<Protected><Invoices/></Protected>}/>
        <Route path="*" element={<Navigate to="/patients" replace />}/>
      </Routes>
    </div>
  )
}
