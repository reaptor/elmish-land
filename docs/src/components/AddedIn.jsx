export default function AddedIn({version}) {
  const nugetLink = `https://www.nuget.org/packages/elmish-land/${version}`
  return (
    <span
      style={{
        backgroundColor: 'var(--ifm-breadcrumb-item-background-active)',
        borderRadius: '5rem',
        color: 'var(--ifm-menu-color-active)',
        paddingTop: '0.2rem',
        paddingInline: '0.5rem',
        paddingBottom: '0.3rem',
        fontSize: '0.7rem',
        fontWeight: 'bold',
        verticalAlign: 'middle'
      }}>
      <a href={nugetLink} target="blank">Added in {version}</a>
    </span>
  )
}