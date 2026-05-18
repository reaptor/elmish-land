import React, { useState } from 'react';
import clsx from 'clsx';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import FeatureSection from '@site/src/components/HomepageFeatures';
import CounterPreview from '@site/src/components/PreviewComponents/CounterPreview';
import FileTreePreview from '@site/src/components/PreviewComponents/FileTreePreview';
import TypeSafePreview from '@site/src/components/PreviewComponents/TypeSafePreview';

import Heading from '@theme/Heading';
import styles from './index.module.css';

const Logo = require('@site/static/img/logo.svg').default;

function CopyCommand({ command }) {
  const [copied, setCopied] = useState(false);

  const handleCopy = () => {
    navigator.clipboard.writeText(command).then(() => {
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    });
  };

  return (
    <button
      className={styles.copyCommand}
      onClick={handleCopy}
      aria-label={copied ? 'Copied to clipboard' : `Copy "${command}" to clipboard`}
    >
      <span className={styles.copyCommandPrefix}>$</span>
      <span className={styles.copyCommandText}>{command}</span>
      <span className={styles.copyCommandIcon}>
        {copied ? (
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <polyline points="20 6 9 17 4 12" />
          </svg>
        ) : (
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <rect x="9" y="9" width="13" height="13" rx="2" ry="2" />
            <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" />
          </svg>
        )}
      </span>
    </button>
  );
}

function HomepageHeader() {
  const {siteConfig} = useDocusaurusContext();
  return (
    <header className={clsx('hero hero--primary', styles.heroBanner)}>
      <div className="container">
        <Logo className={clsx(styles.logo)} />
        <Heading as="h1" className={styles.heroTitle}>
          Elmish Land
        </Heading>
        <p className={styles.heroSubtitle}>
          Build reliable, type-safe web applications with the power of F# and the Elm Architecture.
        </p>
        <div className={styles.buttons}>
          <Link
            className="button button--primary button--lg"
            to="/docs/getting-started/creating-a-project">
            Get Started
          </Link>
          <Link
            className={clsx('button button--lg', styles.buttonOutline)}
            to="/docs/api-reference/app-config">
            API Reference
          </Link>
        </div>
        <CopyCommand command="dotnet tool install elmish-land && dotnet elmish-land init" />
      </div>
    </header>
  );
}

const counterCode = [
  {
    language: 'fsharp',
    title: 'src/Pages/Page.fs',
    content: `type Model = { Count: int }

type Msg =
    | Increment
    | Decrement

let init () =
    { Count = 0 }, Command.none

let update msg model =
    match msg with
    | Increment -> { model with Count = model.Count + 1 }, Command.none
    | Decrement -> { model with Count = model.Count - 1 }, Command.none

let view model dispatch =
    Html.div [
        Html.button [ prop.onClick (fun _ -> dispatch Decrement); prop.text "-" ]
        Html.span [ prop.text (string model.Count) ]
        Html.button [ prop.onClick (fun _ -> dispatch Increment); prop.text "+" ]
    ]`,
  },
];

const typeSafeCode = [
  {
    language: 'json',
    title: 'src/Pages/Blog/route.json',
    content: `{
  "queryParameters": [
    {
      "type": "int",
      "name": "page"
    }
  ]
}`,
  },
  {
    language: 'fsharp',
    title: 'src/Pages/Blog/Page.fs',
    content: `// Route parameters are parsed and typed automatically
let page (shared: SharedModel) (route: BlogRoute) =
    // route.Page  : int option   (query parameter)
    Page.from init update view () LayoutMsg`,
  },
];

export default function Home() {
  const {siteConfig} = useDocusaurusContext();
  return (
    <Layout
      title={`${siteConfig.tagline}`}
      description={`${siteConfig.tagline}`}>
      <HomepageHeader />
      <main>
        <FeatureSection
          heading="Build pages with the Elm Architecture"
          description="Each page is a self-contained module with its own model, update, and view functions. Simple, predictable, and easy to reason about."
          codeBlocks={counterCode}
          PreviewComponent={CounterPreview}
          backgroundVariant="alt"
          ctaLink="/docs/core-concepts/pages"
          ctaText="Learn about pages"
        />
        <FeatureSection
          heading="Route pages with the file system"
          description="Just create files in the Pages directory and Elmish Land automatically generates routes. No manual route configuration needed."
          PreviewComponent={FileTreePreview}
          ctaLink="/docs/core-concepts/routing"
          ctaText="Learn about routing"
        />
        <FeatureSection
          heading="Type-safe from URL to page"
          description="Define your route parameters in a simple JSON file and get fully typed, parsed route objects in your page functions. No string parsing, no runtime errors."
          codeBlocks={typeSafeCode}
          PreviewComponent={TypeSafePreview}
          backgroundVariant="alt"
          ctaLink="/docs/core-concepts/routing#type-safe-routing"
          ctaText="Learn about type-safe routing"
        />
      </main>
    </Layout>
  );
}
