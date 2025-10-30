import React, { useEffect, useState } from 'react'
import { getPaged } from '../lib/api'
import Paginator from '../components/Paginator'

export default function Patients(){
  const [data, setData] = useState({ items:[], page:1, pageSize:20, totalCount:0 })
  const [q, setQ] = useState('')
  const [sortBy, setSort] = useState('FullName')
  const [sortDir, setDir] = useState('asc')

  const load = async (page=1) => {
    const url = `/api/patients?q=${encodeURIComponent(q)}&page=${page}&pageSize=${data.pageSize}&sortBy=${sortBy}&sortDir=${sortDir}`
    const res = await getPaged(url)
    setData(res)
  }
  useEffect(()=>{ load(1) }, [q, sortBy, sortDir])

  return (
    <div>
      <h3>Pacientes</h3>
      <div style={{display:'flex', gap:8, marginBottom:8}}>
        <input placeholder="Buscar..." value={q} onChange={e=>setQ(e.target.value)} />
        <select value={sortBy} onChange={e=>setSort(e.target.value)}>
          <option>FullName</option><option>DocumentId</option><option>BirthDate</option>
        </select>
        <select value={sortDir} onChange={e=>setDir(e.target.value)}>
          <option>asc</option><option>desc</option>
        </select>
      </div>
      <table border="1" cellPadding="6">
        <thead><tr><th>Nombre</th><th>Documento</th><th>Nacimiento</th><th>Email</th><th>Teléfono</th></tr></thead>
        <tbody>
          {data.items.map(x=>(<tr key={x.id}><td>{x.fullName}</td><td>{x.documentId}</td><td>{x.birthDate ?? ''}</td><td>{x.email ?? ''}</td><td>{x.phone ?? ''}</td></tr>))}
        </tbody>
      </table>
      <Paginator page={data.page} pageSize={data.pageSize} total={data.totalCount} onChange={p=>load(p)} />
    </div>
  )
}
