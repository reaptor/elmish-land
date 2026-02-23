import React from 'react';
import clsx from 'clsx';
import Heading from '@theme/Heading';
import Link from '@docusaurus/Link';
import CodeBlock from '@theme/CodeBlock';
import styles from './styles.module.css';

export default function FeatureSection({
  heading,
  description,
  codeBlocks,
  PreviewComponent,
  backgroundVariant = 'default',
  ctaLink,
  ctaText,
}) {
  return (
    <section
      className={clsx(
        styles.featureSection,
        backgroundVariant === 'alt' && styles.featureSectionAlt
      )}
    >
      <div className="container">
        <div className={styles.sectionHeader}>
          <Heading as="h2" className={styles.sectionHeading}>
            {heading}
          </Heading>
          <p className={styles.sectionDescription}>{description}</p>
        </div>
        <div className={clsx(styles.sectionGrid, !codeBlocks && styles.sectionGridCentered)}>
          {codeBlocks && (
            <div className={styles.codeColumn}>
              {codeBlocks.map((block, i) => (
                <CodeBlock key={i} language={block.language} title={block.title}>
                  {block.content}
                </CodeBlock>
              ))}
            </div>
          )}
          <div className={styles.previewColumn}>
            <PreviewComponent />
          </div>
        </div>
        {ctaLink && ctaText && (
          <div className={styles.sectionCta}>
            <Link className="button button--primary button--md" to={ctaLink}>
              {ctaText}
            </Link>
          </div>
        )}
      </div>
    </section>
  );
}
