export default function ExportButton({ onClick, label = 'Export JSON' }) {
  return <button onClick={onClick}>{label}</button>
}
