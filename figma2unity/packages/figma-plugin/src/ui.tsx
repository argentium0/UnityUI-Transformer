import { render } from 'preact';
import { useState, useEffect } from 'preact/hooks';
import { ZipPackager } from './zip-packager.js';
import type { ExportedAsset } from './asset-exporter.js';

export interface BreakdownSummary {
  totalNodes: number;
  supportedCount: number;
  rasterCount: number;
  vectorCount: number;
  unsupportedCount: number;
  nodeCounts: Record<string, number>;
}

function App() {
  const [loading, setLoading] = useState(false);
  const [summary, setSummary] = useState<BreakdownSummary | null>(null);
  const [downloadedFile, setDownloadedFile] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const [syncMode, setSyncMode] = useState<'ZIP' | 'LIVE'>('ZIP');
  const [syncedStatus, setSyncedStatus] = useState<string | null>(null);

  useEffect(() => {
    window.onmessage = async (event) => {
      const msg = event.data.pluginMessage;
      if (!msg) return;

      if (msg.type === 'EXPORT_DATA') {
        try {
          if (syncMode === 'LIVE') {
            // Live mode: POST directly to Fastify bridge server on localhost:3000
            const assetsPayload = (msg.assets || []).map((a: any) => {
              const uint8 = new Uint8Array(a.data);
              let binary = '';
              for (let i = 0; i < uint8.byteLength; i++) {
                binary += String.fromCharCode(uint8[i]);
              }
              const base64 = btoa(binary);
              return {
                path: a.path,
                data: base64,
              };
            });

            const packageName = msg.fileName ? msg.fileName.replace(/\.f2u\.zip$/i, '') : 'FigmaSyncPackage';

            const res = await fetch('http://localhost:3000/sync', {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify({
                packageName,
                document: msg.document,
                assets: assetsPayload,
              }),
            });

            const resData = await res.json();
            if (res.ok && resData.success) {
              setLoading(false);
              setSummary(msg.summary);
              setSyncedStatus(`Synced ${resData.filesWritten} files directly to Unity project at ${resData.outputPath}`);
            } else {
              throw new Error(resData.message || 'Server error');
            }
          } else {
            // Zip mode: Create .f2u.zip archive inside the UI iframe browser environment
            const assets: ExportedAsset[] = (msg.assets || []).map((a: any) => ({
              path: a.path,
              data: new Uint8Array(a.data),
              mimeType: a.mimeType,
            }));

            const zipBytes = ZipPackager.createF2uZip(msg.document, assets);

            // Trigger browser Blob download of the .f2u.zip archive
            const blob = new Blob([zipBytes], { type: 'application/zip' });
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = msg.fileName;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            URL.revokeObjectURL(url);

            setLoading(false);
            setSummary(msg.summary);
            setDownloadedFile(msg.fileName);
          }
        } catch (err: any) {
          setLoading(false);
          setError(`Failed sync operation: ${err?.message || String(err)}`);
        }
      } else if (msg.type === 'TRAVERSAL_ERROR') {
        setLoading(false);
        setError(msg.error);
      }
    };
  }, [syncMode]);

  const handleSync = () => {
    setLoading(true);
    setError(null);
    setDownloadedFile(null);
    parent.postMessage({ pluginMessage: { type: 'START_SYNC' } }, '*');
  };

  return (
    <div style={{ fontFamily: 'sans-serif', padding: 16, background: '#1e1e1e', color: '#fff', boxSizing: 'border-box' }}>
      <h2 style={{ margin: '0 0 8px 0', fontSize: 18, color: '#0d99ff' }}>Figma2Unity Importer</h2>
      <p style={{ fontSize: 12, color: '#aaa', margin: '0 0 12px 0' }}>
        Export design selection to IR JSON document and assets.
      </p>

      <div style={{ marginBottom: 16, fontSize: 12 }}>
        <label style={{ marginRight: 16, cursor: 'pointer' }}>
          <input
            type="radio"
            name="syncMode"
            value="ZIP"
            checked={syncMode === 'ZIP'}
            onChange={() => setSyncMode('ZIP')}
            style={{ marginRight: 6 }}
          />
          Zip Download Mode
        </label>
        <label style={{ cursor: 'pointer' }}>
          <input
            type="radio"
            name="syncMode"
            value="LIVE"
            checked={syncMode === 'LIVE'}
            onChange={() => setSyncMode('LIVE')}
            style={{ marginRight: 6 }}
          />
          Live Localhost Mode
        </label>
      </div>

      <button
        id="sync-button"
        onClick={handleSync}
        disabled={loading}
        style={{
          width: '100%',
          padding: '10px 16px',
          background: loading ? '#555' : '#0d99ff',
          color: '#fff',
          border: 'none',
          borderRadius: 6,
          fontWeight: 600,
          cursor: loading ? 'not-allowed' : 'pointer',
        }}
      >
        {loading ? (syncMode === 'LIVE' ? 'POSTing to localhost:3000...' : 'Packing Zip...') : (syncMode === 'LIVE' ? 'Sync Live to Localhost' : 'Sync & Download Zip')}
      </button>

      {downloadedFile && (
        <div style={{ marginTop: 12, padding: 8, background: '#1b3823', border: '1px solid #4caf50', borderRadius: 6, fontSize: 12, color: '#81c784' }}>
          ✓ Downloaded <strong>{downloadedFile}</strong>
        </div>
      )}

      {syncedStatus && (
        <div style={{ marginTop: 12, padding: 8, background: '#1b3823', border: '1px solid #4caf50', borderRadius: 6, fontSize: 12, color: '#81c784' }}>
          ✓ {syncedStatus}
        </div>
      )}

      {error && (
        <div style={{ marginTop: 12, padding: 10, background: '#3b1c1c', border: '1px solid #ff4d4d', borderRadius: 6, fontSize: 12 }}>
          <strong>Error:</strong> {error}
        </div>
      )}

      {summary && (
        <div style={{ marginTop: 16, padding: 12, background: '#2a2a2a', borderRadius: 6, fontSize: 12 }}>
          <h3 style={{ margin: '0 0 12px 0', fontSize: 14, color: '#0d99ff' }}>Figma Coverage Breakdown</h3>
          
          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6 }}>
            <span>Total Nodes Processed:</span>
            <strong id="total-nodes">{summary.totalNodes}</strong>
          </div>

          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6, color: '#81c784' }}>
            <span>Native UI Toolkit Nodes:</span>
            <strong id="supported-count">{summary.supportedCount}</strong>
          </div>

          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6, color: '#ffb74d' }}>
            <span>Rasterized Images (PNG 1x/2x/3x):</span>
            <strong id="raster-count">{summary.rasterCount}</strong>
          </div>

          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6, color: '#64b5f6' }}>
            <span>Vector Assets (SVG):</span>
            <strong id="vector-count">{summary.vectorCount}</strong>
          </div>

          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6, color: summary.unsupportedCount > 0 ? '#e57373' : '#aaa' }}>
            <span>Unsupported Nodes:</span>
            <strong id="unsupported-count">{summary.unsupportedCount}</strong>
          </div>

          <h4 style={{ margin: '14px 0 6px 0', fontSize: 12, color: '#ccc' }}>Layer Type Summary:</h4>
          <ul style={{ margin: 0, paddingLeft: 16, color: '#bbb' }}>
            {Object.entries(summary.nodeCounts).map(([type, count]) => (
              <li key={type}>
                {type}: {count}
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}

const root = document.getElementById('app');
if (root) {
  render(<App />, root);
}
