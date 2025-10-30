import React from 'react'
export default function Paginator({ page, pageSize, total, onChange }){
  const pages = Math.max(1, Math.ceil(total / pageSize))
  const prev = () => onChange(Math.max(1, page-1))
  const next = () => onChange(Math.min(pages, page+1))
  return (
    <div style={{display:'flex', gap:8, alignItems:'center'}}>
      <button onClick={prev} disabled={page<=1}>Prev</button>
      <span>Página {page} / {pages}</span>
      <button onClick={next} disabled={page>=pages}>Next</button>
    </div>
  )
}
