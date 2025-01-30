import clsx from 'clsx';
import Heading from '@theme/Heading';
import styles from './styles.module.css';

const FeatureList = [
  {
    url: '/docs/getting-started/creating-a-project',
    title: 'Easy to Use',
    // Svg: require('@site/static/img/undraw_docusaurus_mountain.svg').default,
    description: (
      <>
        Elmish Land was designed from the ground up to be easily installed and
        used to get your web app up and running quickly.
      </>
    ),
  },
  {
    url: '/docs/core-concepts/pages#routing',
    title: 'File based router',
    // Svg: require('@site/static/img/undraw_docusaurus_tree.svg').default,
    description: (
      <>
        The router removes the hurde of manually hooking up all your pages and URLs.
        Elmish Land does this automatically for you based on file based conventions.
      </>
    ),
  },
  {
    url: '/docs/core-concepts/pages#type-safe-routing',
    title: 'Type-Safe Routing',
    // Svg: require('@site/static/img/undraw_docusaurus_react.svg').default,
    description: (
      <>
        Use type-safe route and query parameters. This ensures that route parameters
        are correctly typed and parsed, reducing runtime errors and improving code reliability.
      </>
    ),
  },
];

function Feature({url, title, description}) {
  return (
    <div className={clsx('col col--4')}>
      {/* <div className="text--center">
        <Svg className={styles.featureSvg} role="img" />
      </div> */}
      <div className="text--center padding-horiz--md">
        <a href={url} className='featureLink'>
            <Heading as="h3">{title}</Heading>
            <p>{description}</p>
        </a>
      </div>
    </div>
  );
}

export default function HomepageFeatures() {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}
