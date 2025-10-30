import React, { useEffect, useState } from 'react'
import { getPaged } from '../lib/api'
import Paginator from '../components/Paginator'

export default function Invoices(){
  const [data, setData] = useState({ items:[], page:1, pageSize:20, totalCount:0 })
  const [paid, setPaid] = useState('')
  const [sortBy, setSort] = useState('IssuedAt')
  const [sortDir, setDir] = useState('desc')

  const load = async (page=1) => {
    const qs = new URLSearchParams()
    if(paid) qs.set('paid', paid)
    qs.set('page', page); qs.set('pageSize', data.pageSize)
    qs.set('sortBy', sortBy); qs.set('sortDir', sortDir)
    const res = await getPaged(`/api/invoices?${qs.toString()}`)
    setData(res)
  }
  useEffect(()=>{ load(1) }, [paid, sortBy, sortDir])

  return (
    <div>
      <h3>Facturas</h3>
      <div style={{display:'flex', gap:8, marginBottom:8}}>
        <select value={paid} onChange={e=>setPaid(e.target.value)}>
          <option value="">Todas</option>
          <option value="true">Pagadas</option>
          <option value="false">Pendientes</option>
        </select>
        <select value={sortBy} onChange={e=>setSort(e.target.value)}>
          <option>IssuedAt</option><option>Number</option><option>Amount</option><option>Paid</option>
        </select>
        <select value={sortDir} onChange={e=>setDir(e.target.value)}>
          <option>asc</option><option>desc</option>
        </select>
      </div>
      <table border="1" cellPadding="6">
        <thead><tr><th>Número</th><th>Paciente</th><th>Monto</th><th>Fecha</th><th>Estado</th></tr></thead>
        <tbody>
          {data.items.map(x=>(<tr key={x.id}><td>{x.number}</td><td>{x.patientName}</td><td>{x.amount}</td><td>{x.issuedAt}</td><td>{x.paid?'Pagada':'Pendiente'}</td></tr>))}
        </tbody>
      </table>
      <Paginator page={data.page} pageSize={data.pageSize} total={data.totalCount} onChange={p=>load(p)} />
    </div>
  )
}
