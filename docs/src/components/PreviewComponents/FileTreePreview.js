import React from 'react';
import styles from './preview.module.css';

const files = [
  { indent: 0, icon: '\uD83D\uDCC2', name: 'src/Pages/', isDir: true },
  { indent: 1, icon: '\uD83D\uDCC4', name: 'Page.fs', route: '/' },
  { indent: 1, icon: '\uD83D\uDCC2', name: 'About/', isDir: true },
  { indent: 2, icon: '\uD83D\uDCC4', name: 'Page.fs', route: '/about' },
  { indent: 1, icon: '\uD83D\uDCC2', name: 'Blog/', isDir: true },
  { indent: 2, icon: '\uD83D\uDCC2', name: 'Post/_Slug/', isDir: true },
  { indent: 3, icon: '\uD83D\uDCC4', name: 'Page.fs', route: '/blog/:slug' },
];

export default function FileTreePreview() {
  return (
    <div className={styles.browserFrame}>
      <div className={styles.titleBar}>
        <span className={`${styles.dot} ${styles.dotRed}`} />
        <span className={`${styles.dot} ${styles.dotYellow}`} />
        <span className={`${styles.dot} ${styles.dotGreen}`} />
        <span className={styles.titleBarText}>Project Structure</span>
      </div>
      <div className={styles.previewBody}>
        <div className={styles.fileTreeLayout}>
          {files.map((file, i) => (
            <div key={i} className={styles.fileTreeRow}>
              <div className={styles.fileTreeItem}>
                {Array.from({ length: file.indent }).map((_, j) => (
                  <span key={j} className={styles.fileTreeIndent} />
                ))}
                <span className={styles.fileTreeIcon}>{file.icon}</span>
                <span>{file.name}</span>
              </div>
              {file.route && (
                <div className={styles.routeArrow}>
                  <span className={styles.routeArrowIcon}>&rarr;</span>
                  <span className={styles.routePath}>{file.route}</span>
                </div>
              )}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
