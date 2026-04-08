import SpanNode from './SpanNode'

export default function SpanTree({ spans }) {
  if (!spans || spans.length === 0) return null

  return (
    <div>
      {spans.map(span => (
        <SpanNode key={span.id} span={span} />
      ))}
    </div>
  )
}
