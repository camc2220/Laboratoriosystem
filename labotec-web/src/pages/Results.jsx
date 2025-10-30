import React, { useEffect, useState } from 'react'
import { getPaged, postForm } from '../lib/api'
import Paginator from '../components/Paginator'

export default function Results(){
  const [data, setData] = useState({ items:[], page:1, pageSize:20, totalCount:0 })
  const [patientId, setPid] = useState('')
  const [test, setTest] = useState('')
  const [sortBy, setSort] = useState('ReleasedAt')
  const [sortDir, setDir] = useState('desc')
  const [uploading, setUploading] = useState(null)

  const load = async (page=1) => {
    const params = new URLSearchParams()
    if(patientId) params.set('patientId', patientId)
    if(test) params.set('test', test)
    params.set('page', page); params.set('pageSize', data.pageSize)
    params.set('sortBy', sortBy); params.set('sortDir', sortDir)
    const res = await getPaged(`/api/results?${params.toString()}`)
    setData(res)
  }
  useEffect(()=>{ load(1) }, [patientId, test, sortBy, sortDir])

  const uploadPdf = async (id, file) => {
    const form = new FormData()
    form.append('file', file)
    await postForm(`/api/results/${id}/pdf`, form)
    await load(data.page)
  }

  return (
    <div>
      <h3>Resultados</h3>
      <div style={{display:'flex', gap:8, marginBottom:8}}>
        <input placeholder="PatientId" value={patientId} onChange={e=>setPid(e.target.value)} />
        <input placeholder="Test" value={test} onChange={e=>setTest(e.target.value)} />
        <select value={sortBy} onChange={e=>setSort(e.target.value)}>
          <option>ReleasedAt</option><option>TestName</option>
        </select>
        <select value={sortDir} onChange={e=>setDir(e.target.value)}>
          <option>asc</option><option>desc</option>
        </select>
      </div>
      <table border="1" cellPadding="6">
        <thead><tr><th>Paciente</th><th>Prueba</th><th>Valor</th><th>Fecha</th><th>PDF</th></tr></thead>
        <tbody>
          {data.items.map(x=>(
            <tr key={x.id}>
              <td>{x.patientName}</td><td>{x.testName}</td><td>{x.resultValue} {x.unit}</td><td>{x.releasedAt}</td>
              <td>
                <input type="file" accept="application/pdf" onChange={e=>uploadPdf(x.id, e.target.files[0])} />
                {x.pdfUrl && <a href={x.pdfUrl} target="_blank">ver</a>}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      <Paginator page={data.page} pageSize={data.pageSize} total={data.totalCount} onChange={p=>load(p)} />
    </div>
  )
}
