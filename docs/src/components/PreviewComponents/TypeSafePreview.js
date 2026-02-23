import React from 'react';
import styles from './preview.module.css';

export default function TypeSafePreview() {
  return (
    <div className={styles.browserFrame}>
      <div className={styles.titleBar}>
        <span className={`${styles.dot} ${styles.dotRed}`} />
        <span className={`${styles.dot} ${styles.dotYellow}`} />
        <span className={`${styles.dot} ${styles.dotGreen}`} />
        <span className={styles.titleBarText}>Type-Safe Routes</span>
      </div>
      <div className={styles.previewBody}>
        <div className={styles.typeSafeLayout}>
          <div className={styles.urlBar}>
            <span className={styles.urlLock}>{'\uD83D\uDD12'}</span>
            <span className={styles.urlText}>
              myapp.com/blog?<span className={styles.urlParam}>page=2</span>
            </span>
          </div>
          <div className={styles.typedRouteBox}>
            <div className={styles.typedRouteTitle}>Parsed Route</div>
            <div className={styles.typedField}>
              <span className={styles.typedFieldName}>page</span>
              <span className={styles.typedFieldType}>{' : int option'}</span>
              <span>{' = '}</span>
              <span className={styles.typedFieldValue}>Some 2</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
