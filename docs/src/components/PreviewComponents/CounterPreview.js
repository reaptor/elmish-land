import React, { useState } from 'react';
import styles from './preview.module.css';

export default function CounterPreview() {
  const [count, setCount] = useState(0);

  return (
    <div className={styles.browserFrame}>
      <div className={styles.titleBar}>
        <span className={`${styles.dot} ${styles.dotRed}`} />
        <span className={`${styles.dot} ${styles.dotYellow}`} />
        <span className={`${styles.dot} ${styles.dotGreen}`} />
        <span className={styles.titleBarText}>localhost:5173</span>
      </div>
      <div className={styles.previewBody}>
        <div className={styles.counterUI}>
          <button
            className={styles.counterBtn}
            onClick={() => setCount(c => c - 1)}
            aria-label="Decrement"
          >
            -
          </button>
          <span className={styles.counterValue}>{count}</span>
          <button
            className={styles.counterBtn}
            onClick={() => setCount(c => c + 1)}
            aria-label="Increment"
          >
            +
          </button>
        </div>
      </div>
    </div>
  );
}
