import React, { useState, useEffect, useCallback, useRef } from "react";
import {
  Activity, AlertTriangle, Bell, BellRing, CheckCircle, ChevronDown, ChevronRight,
  Cloud, CloudRain, CloudSnow, Droplets, Eye, Gauge, Globe, Layers,
  LayoutDashboard, MapPin, Monitor, Radio, RefreshCw, Server,
  Sun, Thermometer, TrendingUp, Wind, Zap, ZapOff, BarChart3,
  Shield, ShieldAlert, ShieldCheck, ShieldX, Clock, ArrowUpRight,
  ArrowDownRight, Minus, CircleDot, Wifi, WifiOff, Settings,
  ChevronUp, X, Info, AlertCircle
} from "lucide-react";
import L from "leaflet";
import "leaflet/dist/leaflet.css";

// ==================== CONFIG ====================
const API_BASE = "";  // prazan jer koristimo proxy
const POLL_INTERVAL = 3000;

// ==================== WCF DATE PARSER ====================
function parseWcfDate(val) {
  if (!val) return "";
  if (typeof val === "string" && val.includes("/Date(")) {
    const m = val.match(/\/Date\((\d+)\)\//);
    if (m) return new Date(parseInt(m[1])).toLocaleTimeString("sr-RS");
  }
  if (typeof val === "string" && val.includes("T")) {
    return new Date(val).toLocaleTimeString("sr-RS");
  }
  return String(val);
}

// ==================== API LAYER ====================
async function fetchApi(path) {
  try {
    const res = await fetch(`${API_BASE}${path}`);
    if (!res.ok) return null;
    return await res.json();
  } catch {
    return null;
  }
}

// ==================== SEVERITY UTILS ====================
const severityConfig = {
  Critical: { bg: "rgba(220,38,38,0.12)", border: "#dc2626", text: "#fca5a5", icon: ShieldX },
  High: { bg: "rgba(249,115,22,0.12)", border: "#f97316", text: "#fdba74", icon: ShieldAlert },
  Medium: { bg: "rgba(234,179,8,0.12)", border: "#eab308", text: "#fde047", icon: AlertTriangle },
  Low: { bg: "rgba(34,197,94,0.12)", border: "#22c55e", text: "#86efac", icon: Info },
};

const statusConfig = {
  Critical: { color: "#ef4444", glow: "0 0 20px rgba(239,68,68,0.4)", icon: ZapOff },
  Warning: { color: "#f59e0b", glow: "0 0 20px rgba(245,158,11,0.4)", icon: AlertTriangle },
  Normal: { color: "#10b981", glow: "0 0 20px rgba(16,185,129,0.4)", icon: ShieldCheck },
};

// ==================== LEAFLET MAP COMPONENT ====================
function GridMap({ nodes, onNodeClick, selectedNode }) {
  const mapRef = useRef(null);
  const mapInstanceRef = useRef(null);
  const markersRef = useRef([]);
  const linesRef = useRef([]);

  useEffect(() => {
    if (mapInstanceRef.current || !mapRef.current) return;

    const map = L.map(mapRef.current, {
      zoomControl: false,
      attributionControl: false,
    }).setView([44.2, 20.5], 7);

    L.tileLayer("https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png", {
      maxZoom: 19,
    }).addTo(map);

    L.control.zoom({ position: "bottomright" }).addTo(map);
    mapInstanceRef.current = map;

    return () => { map.remove(); mapInstanceRef.current = null; };
  }, []);

  useEffect(() => {
    const map = mapInstanceRef.current;
    if (!map || !nodes || nodes.length === 0) return;

    markersRef.current.forEach(m => map.removeLayer(m));
    linesRef.current.forEach(l => map.removeLayer(l));
    markersRef.current = [];
    linesRef.current = [];

    const supplier = nodes.find(n => n.Role === "Supplier");
    const consumers = nodes.filter(n => n.Role === "Consumer");

    if (supplier) {
      consumers.forEach(consumer => {
        const sc = statusConfig[consumer.Status] || statusConfig.Normal;
        const line = L.polyline(
          [[supplier.Latitude, supplier.Longitude], [consumer.Latitude, consumer.Longitude]],
          {
            color: sc.color,
            weight: 3,
            opacity: 0.7,
            dashArray: consumer.Status === "Critical" ? "10 5" : null,
          }
        ).addTo(map);
        linesRef.current.push(line);
      });
    }

    nodes.forEach(node => {
      const sc = statusConfig[node.Status] || statusConfig.Normal;
      const isSelected = selectedNode === node.NodeId;
      const size = node.Role === "Supplier" ? 18 : 14;

      const icon = L.divIcon({
        className: "",
        html: `<div style="
          width:${size * 2}px;height:${size * 2}px;border-radius:50%;
          background:${sc.color};
          box-shadow:${sc.glow}${isSelected ? `, 0 0 0 4px rgba(255,255,255,0.5)` : ""};
          display:flex;align-items:center;justify-content:center;
          border:2px solid rgba(255,255,255,0.3);
          cursor:pointer;
          ${node.Status === "Critical" ? "animation: pulse 1.5s infinite;" : ""}
        ">
          <span style="color:white;font-size:${size - 2}px;font-weight:700;">
            ${node.Role === "Supplier" ? "⚡" : node.NodeId.slice(-1)}
          </span>
        </div>`,
        iconSize: [size * 2, size * 2],
        iconAnchor: [size, size],
      });

      const marker = L.marker([node.Latitude, node.Longitude], { icon })
        .addTo(map)
        .on("click", () => onNodeClick(node.NodeId));

      const popup = L.popup({ className: "grid-popup", offset: [0, -size] }).setContent(`
        <div style="font-family:'DM Sans',sans-serif;color:#e2e8f0;min-width:160px;">
          <div style="font-weight:700;font-size:14px;margin-bottom:4px;">${node.NodeName}</div>
          <div style="font-size:12px;opacity:0.7;margin-bottom:8px;">${node.City}</div>
          <div style="font-size:12px;display:grid;gap:3px;">
            <span>Voltage: ${node.Voltage}V</span>
            <span>Current: ${node.Current}A</span>
            <span>Power: ${node.Power}W</span>
            <span>Freq: ${node.Frequency}Hz</span>
          </div>
        </div>
      `);
      marker.bindPopup(popup);
      markersRef.current.push(marker);
    });
  }, [nodes, selectedNode, onNodeClick]);

  return (
    <div style={{ position: "relative", width: "100%", height: "100%" }}>
      <div ref={mapRef} style={{ width: "100%", height: "100%", borderRadius: 12 }} />
      <style>{`
        .grid-popup .leaflet-popup-content-wrapper {
          background: rgba(15,23,42,0.95);
          border: 1px solid rgba(100,150,255,0.2);
          border-radius: 10px;
          box-shadow: 0 8px 32px rgba(0,0,0,0.4);
        }
        .grid-popup .leaflet-popup-tip { background: rgba(15,23,42,0.95); }
        @keyframes pulse {
          0%, 100% { opacity: 1; transform: scale(1); }
          50% { opacity: 0.7; transform: scale(1.15); }
        }
      `}</style>
    </div>
  );
}

// ==================== STAT CARD ====================
function StatCard({ icon: Icon, label, value, sub, accent = "#64a0ff" }) {
  return (
    <div style={{
      background: "rgba(15,23,42,0.6)",
      border: "1px solid rgba(100,150,255,0.1)",
      borderRadius: 10,
      padding: "14px 16px",
      display: "flex",
      alignItems: "center",
      gap: 12,
    }}>
      <div style={{
        width: 40, height: 40, borderRadius: 10,
        background: `${accent}15`,
        display: "flex", alignItems: "center", justifyContent: "center",
        flexShrink: 0,
      }}>
        <Icon size={20} color={accent} />
      </div>
      <div style={{ minWidth: 0 }}>
        <div style={{ fontSize: 11, color: "#94a3b8", letterSpacing: "0.5px", textTransform: "uppercase" }}>{label}</div>
        <div style={{ fontSize: 20, fontWeight: 700, color: "#f1f5f9", lineHeight: 1.2 }}>{value}</div>
        {sub && <div style={{ fontSize: 11, color: "#64748b", marginTop: 2 }}>{sub}</div>}
      </div>
    </div>
  );
}

// ==================== NODE DETAIL PANEL ====================
function NodeDetail({ node, weather }) {
  if (!node) return (
    <div style={{
      height: "100%", display: "flex", alignItems: "center", justifyContent: "center",
      flexDirection: "column", gap: 8, color: "#475569", padding: 20,
    }}>
      <MapPin size={32} />
      <span style={{ fontSize: 13 }}>Klikni na node na mapi</span>
    </div>
  );

  const sc = statusConfig[node.Status] || statusConfig.Normal;
  const StatusIcon = sc.icon;
  const cityWeather = weather?.find(w => w.City === node.City);

  return (
    <div style={{ padding: 16, display: "flex", flexDirection: "column", gap: 12 }}>
      <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
        <div style={{
          width: 36, height: 36, borderRadius: "50%",
          background: sc.color, boxShadow: sc.glow,
          display: "flex", alignItems: "center", justifyContent: "center",
        }}>
          <StatusIcon size={18} color="white" />
        </div>
        <div>
          <div style={{ fontWeight: 700, fontSize: 15, color: "#f1f5f9" }}>{node.NodeName}</div>
          <div style={{ fontSize: 12, color: "#64748b" }}>{node.City} | {node.Role}</div>
        </div>
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 8 }}>
        {[
          { icon: Zap, label: "Voltage", value: `${node.Voltage}V`, color: "#fbbf24" },
          { icon: Activity, label: "Current", value: `${node.Current}A`, color: "#60a5fa" },
          { icon: Gauge, label: "Power", value: `${node.Power}W`, color: "#f97316" },
          { icon: Radio, label: "Frequency", value: `${node.Frequency}Hz`, color: "#a78bfa" },
        ].map(item => (
          <div key={item.label} style={{
            background: "rgba(15,23,42,0.5)", borderRadius: 8, padding: "10px 12px",
            border: "1px solid rgba(100,150,255,0.08)",
          }}>
            <div style={{ display: "flex", alignItems: "center", gap: 6, marginBottom: 4 }}>
              <item.icon size={13} color={item.color} />
              <span style={{ fontSize: 11, color: "#94a3b8" }}>{item.label}</span>
            </div>
            <div style={{ fontSize: 16, fontWeight: 700, color: "#f1f5f9" }}>{item.value}</div>
          </div>
        ))}
      </div>

      {cityWeather && cityWeather.Description !== "Nedostupno" && (
        <div style={{
          background: "rgba(15,23,42,0.5)", borderRadius: 8, padding: 12,
          border: "1px solid rgba(100,150,255,0.08)",
        }}>
          <div style={{ display: "flex", alignItems: "center", gap: 6, marginBottom: 8 }}>
            <Cloud size={14} color="#38bdf8" />
            <span style={{ fontSize: 12, fontWeight: 600, color: "#94a3b8" }}>Vreme</span>
          </div>
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 6, fontSize: 12 }}>
            <div style={{ display: "flex", alignItems: "center", gap: 4, color: "#e2e8f0" }}>
              <Thermometer size={12} color="#ef4444" /> {cityWeather.Temperature}°C
            </div>
            <div style={{ display: "flex", alignItems: "center", gap: 4, color: "#e2e8f0" }}>
              <Droplets size={12} color="#38bdf8" /> {cityWeather.Humidity}%
            </div>
            <div style={{ display: "flex", alignItems: "center", gap: 4, color: "#e2e8f0" }}>
              <Wind size={12} color="#94a3b8" /> {cityWeather.WindSpeed} m/s
            </div>
            <div style={{ display: "flex", alignItems: "center", gap: 4, color: "#e2e8f0" }}>
              <Sun size={12} color="#fbbf24" /> {cityWeather.Description}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// ==================== ANOMALY TABLE ====================
function AnomalyTable({ anomalies }) {
  const [filter, setFilter] = useState("All");
  const types = ["All", ...new Set((anomalies || []).map(a => a.AnomalyType))];
  const filtered = filter === "All" ? anomalies : anomalies?.filter(a => a.AnomalyType === filter);

  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column" }}>
      <div style={{
        display: "flex", alignItems: "center", gap: 8, padding: "12px 16px",
        borderBottom: "1px solid rgba(100,150,255,0.08)", flexWrap: "wrap",
      }}>
        {types.map(t => (
          <button key={t} onClick={() => setFilter(t)} style={{
            padding: "4px 10px", borderRadius: 6, border: "none",
            background: filter === t ? "rgba(100,150,255,0.2)" : "transparent",
            color: filter === t ? "#93c5fd" : "#64748b",
            fontSize: 11, fontWeight: 600, cursor: "pointer",
          }}>{t}</button>
        ))}
      </div>
      <div style={{ flex: 1, overflow: "auto", padding: "0 8px" }}>
        {(!filtered || filtered.length === 0) ? (
          <div style={{
            display: "flex", alignItems: "center", justifyContent: "center",
            height: "100%", color: "#475569", fontSize: 13,
          }}>
            <ShieldCheck size={16} style={{ marginRight: 6 }} /> Nema anomalija
          </div>
        ) : (
          <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 12 }}>
            <thead>
              <tr style={{ color: "#64748b", textAlign: "left" }}>
                <th style={{ padding: "8px 6px", fontWeight: 600 }}>Vreme</th>
                <th style={{ padding: "8px 6px", fontWeight: 600 }}>Tip</th>
                <th style={{ padding: "8px 6px", fontWeight: 600 }}>Severity</th>
                <th style={{ padding: "8px 6px", fontWeight: 600 }}>Vrednost</th>
                <th style={{ padding: "8px 6px", fontWeight: 600 }}>Opis</th>
              </tr>
            </thead>
            <tbody>
              {filtered.slice(0, 100).map((a, i) => {
                const sev = severityConfig[a.Severity] || severityConfig.Low;
                const SevIcon = sev.icon;
                return (
                  <tr key={i} style={{
                    borderBottom: "1px solid rgba(100,150,255,0.04)",
                  }}
                    onMouseEnter={e => e.currentTarget.style.background = "rgba(100,150,255,0.04)"}
                    onMouseLeave={e => e.currentTarget.style.background = "transparent"}
                  >
                    <td style={{ padding: "8px 6px", color: "#94a3b8", whiteSpace: "nowrap" }}>
                      {parseWcfDate(a.DetectedAt)}
                    </td>
                    <td style={{ padding: "8px 6px" }}>
                      <span style={{
                        padding: "2px 8px", borderRadius: 4,
                        background: "rgba(100,150,255,0.1)", color: "#93c5fd",
                        fontSize: 11, fontWeight: 600,
                      }}>{a.AnomalyType}</span>
                    </td>
                    <td style={{ padding: "8px 6px" }}>
                      <div style={{ display: "flex", alignItems: "center", gap: 4 }}>
                        <SevIcon size={13} color={sev.text} />
                        <span style={{ color: sev.text, fontWeight: 600 }}>{a.Severity}</span>
                      </div>
                    </td>
                    <td style={{ padding: "8px 6px", color: "#e2e8f0", fontFamily: "monospace", fontSize: 11 }}>
                      {typeof a.Value === "number" ? a.Value.toFixed(2) : a.Value}
                    </td>
                    <td style={{ padding: "8px 6px", color: "#94a3b8", maxWidth: 250, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
                      {a.Description}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}

// ==================== WEATHER CORRELATION ====================
function WeatherCorrelation({ correlation }) {
  if (!correlation || !correlation.NodeCorrelations) return (
    <div style={{ display: "flex", alignItems: "center", justifyContent: "center", height: "100%", color: "#475569", fontSize: 13 }}>
      <Cloud size={16} style={{ marginRight: 6 }} /> Ucitavanje korelacije...
    </div>
  );

  return (
    <div style={{ padding: 12, display: "flex", flexDirection: "column", gap: 8 }}>
      {correlation.NodeCorrelations.map((nc, i) => {
        const riskColor = nc.RiskLevel === "High" ? "#ef4444" : nc.RiskLevel === "Medium" ? "#f59e0b" : "#10b981";
        return (
          <div key={i} style={{
            background: "rgba(15,23,42,0.5)", borderRadius: 8, padding: 12,
            border: `1px solid ${riskColor}20`,
          }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 8 }}>
              <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
                <MapPin size={13} color="#64a0ff" />
                <span style={{ fontWeight: 700, fontSize: 13, color: "#f1f5f9" }}>{nc.City}</span>
              </div>
              <span style={{
                padding: "2px 8px", borderRadius: 4, fontSize: 10, fontWeight: 700,
                background: `${riskColor}20`, color: riskColor,
              }}>{nc.RiskLevel} RISK</span>
            </div>
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr", gap: 6, fontSize: 11 }}>
              <div style={{ color: "#94a3b8" }}>
                <Thermometer size={11} style={{ marginRight: 3, verticalAlign: "middle" }} />
                {nc.Temperature}°C
              </div>
              <div style={{ color: "#94a3b8" }}>
                <TrendingUp size={11} style={{ marginRight: 3, verticalAlign: "middle" }} />
                Risk: x{nc.RiskFactor}
              </div>
              <div style={{ color: "#94a3b8" }}>
                <Zap size={11} style={{ marginRight: 3, verticalAlign: "middle" }} />
                ~{nc.EstimatedPowerUsage}W
              </div>
            </div>
          </div>
        );
      })}
    </div>
  );
}

// ==================== NOTIFICATION FEED ====================
function NotificationFeed({ notifications }) {
  if (!notifications || notifications.length === 0) return (
    <div style={{
      display: "flex", alignItems: "center", justifyContent: "center",
      height: "100%", color: "#475569", fontSize: 13,
    }}>
      <Bell size={16} style={{ marginRight: 6 }} /> Nema notifikacija
    </div>
  );

  return (
    <div style={{ padding: "8px 12px", display: "flex", flexDirection: "column", gap: 4, overflow: "auto", height: "100%" }}>
      {notifications.slice().reverse().slice(0, 30).map((n, i) => {
        const sev = severityConfig[n.Severity] || severityConfig.Low;
        const SevIcon = sev.icon;
        return (
          <div key={n.Id || i} style={{
            display: "flex", alignItems: "flex-start", gap: 8, padding: "8px 10px",
            borderRadius: 6, background: sev.bg,
            borderLeft: `3px solid ${sev.border}`,
          }}>
            <SevIcon size={14} color={sev.text} style={{ marginTop: 2, flexShrink: 0 }} />
            <div style={{ flex: 1, minWidth: 0 }}>
              <div style={{ fontSize: 12, color: "#e2e8f0", lineHeight: 1.4 }}>{n.Message}</div>
              <div style={{ fontSize: 10, color: "#64748b", marginTop: 2 }}>
                {parseWcfDate(n.Timestamp)} | {n.Type}
              </div>
            </div>
          </div>
        );
      })}
    </div>
  );
}

// ==================== MAIN DASHBOARD ====================
function App() {
  const [nodes, setNodes] = useState([]);
  const [report, setReport] = useState(null);
  const [anomalies, setAnomalies] = useState([]);
  const [notifications, setNotifications] = useState([]);
  const [weather, setWeather] = useState(null);
  const [correlation, setCorrelation] = useState(null);
  const [selectedNode, setSelectedNode] = useState(null);
  const [connected, setConnected] = useState(false);

  const poll = useCallback(async () => {
    const [n, r, a, notif, w, corr] = await Promise.all([
      fetchApi("/api/grid/nodes"),
      fetchApi("/api/grid/report"),
      fetchApi("/api/grid/anomalies/frequency?threshold=1.0"),
      fetchApi("/api/grid/notifications?count=50"),
      fetchApi("/api/weather/all"),
      fetchApi("/api/weather/correlation"),
    ]);

    if (n) { setNodes(n); setConnected(true); }
    else setConnected(false);
    if (r) setReport(r);
    if (a) {
      const pa = await fetchApi("/api/grid/anomalies/power?threshold=4000");
      setAnomalies([...(a || []), ...(pa || [])]);
    }
    if (notif) setNotifications(notif);
    if (w) setWeather(w);
    if (corr) setCorrelation(corr);
  }, []);

  useEffect(() => {
    poll();
    const interval = setInterval(poll, POLL_INTERVAL);
    return () => clearInterval(interval);
  }, [poll]);

  const selectedNodeData = nodes?.find(n => n.NodeId === selectedNode);
  const criticalCount = nodes?.filter(n => n.Status === "Critical").length || 0;
  const warningCount = nodes?.filter(n => n.Status === "Warning").length || 0;

  return (
    <div style={{
      width: "100vw", height: "100vh", overflow: "hidden",
      background: "linear-gradient(145deg, #020617 0%, #0a1628 30%, #0f172a 60%, #0c1322 100%)",
      fontFamily: "'DM Sans', 'Segoe UI', system-ui, sans-serif",
      color: "#e2e8f0", display: "flex", flexDirection: "column",
    }}>
      <link href="https://fonts.googleapis.com/css2?family=DM+Sans:wght@400;500;600;700&display=swap" rel="stylesheet" />

      {/* HEADER */}
      <header style={{
        height: 56, padding: "0 20px",
        display: "flex", alignItems: "center", justifyContent: "space-between",
        borderBottom: "1px solid rgba(100,150,255,0.08)",
        background: "rgba(10,22,40,0.8)",
        backdropFilter: "blur(12px)",
        flexShrink: 0,
      }}>
        <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
          <div style={{
            width: 34, height: 34, borderRadius: 8,
            background: "linear-gradient(135deg, #1d4ed8, #3b82f6)",
            display: "flex", alignItems: "center", justifyContent: "center",
            boxShadow: "0 0 20px rgba(59,130,246,0.3)",
          }}>
            <Zap size={18} color="white" />
          </div>
          <div>
            <div style={{ fontSize: 16, fontWeight: 700, color: "#f1f5f9", lineHeight: 1.2 }}>Smart Grid Monitor</div>
            <div style={{ fontSize: 10, color: "#64748b", letterSpacing: "1px", textTransform: "uppercase" }}>Serbia Power Network</div>
          </div>
        </div>

        <div style={{ display: "flex", alignItems: "center", gap: 16 }}>
          {report && (
            <div style={{ display: "flex", gap: 12, fontSize: 11 }}>
              <span style={{ color: "#64748b" }}>
                <Monitor size={12} style={{ marginRight: 4, verticalAlign: "middle" }} />
                {report.TotalReadings} zapisa
              </span>
              <span style={{ color: "#10b981" }}>
                <ShieldCheck size={12} style={{ marginRight: 4, verticalAlign: "middle" }} />
                {report.NormalCount} OK
              </span>
              <span style={{ color: "#ef4444" }}>
                <ShieldX size={12} style={{ marginRight: 4, verticalAlign: "middle" }} />
                {report.FaultCount} kvarova
              </span>
            </div>
          )}
          <div style={{
            display: "flex", alignItems: "center", gap: 6,
            padding: "4px 10px", borderRadius: 6,
            background: connected ? "rgba(16,185,129,0.1)" : "rgba(239,68,68,0.1)",
            border: `1px solid ${connected ? "rgba(16,185,129,0.2)" : "rgba(239,68,68,0.2)"}`,
          }}>
            {connected ? <Wifi size={12} color="#10b981" /> : <WifiOff size={12} color="#ef4444" />}
            <span style={{ fontSize: 11, color: connected ? "#10b981" : "#ef4444", fontWeight: 600 }}>
              {connected ? "LIVE" : "OFFLINE"}
            </span>
          </div>
        </div>
      </header>

      {/* MAIN CONTENT */}
      <div style={{
        flex: 1, display: "grid", overflow: "hidden",
        gridTemplateColumns: "1fr 340px",
        gridTemplateRows: "auto 1fr auto",
        gap: 0,
      }}>

        {/* STATS ROW */}
        <div style={{
          gridColumn: "1 / -1", padding: "12px 20px",
          display: "grid", gridTemplateColumns: "repeat(6, 1fr)", gap: 10,
          borderBottom: "1px solid rgba(100,150,255,0.06)",
        }}>
          <StatCard icon={Gauge} label="Avg Power" value={report ? `${report.AvgPower} kW` : "..."} accent="#f97316" />
          <StatCard icon={Zap} label="Avg Voltage" value={report ? `${report.AvgVoltage}V` : "..."} accent="#fbbf24" />
          <StatCard icon={Activity} label="Avg Current" value={report ? `${report.AvgCurrent}A` : "..."} accent="#60a5fa" />
          <StatCard icon={Radio} label="Avg Freq" value={report ? `${report.AvgFrequency} Hz` : "..."} accent="#a78bfa" />
          <StatCard icon={AlertTriangle} label="Freq Anomalies" value={report ? report.FrequencyAnomalyCount : "..."} accent="#ef4444" />
          <StatCard icon={Zap} label="Power Overloads" value={report ? report.PowerOverloadCount : "..."} accent="#f97316" />
        </div>

        {/* MAP */}
        <div style={{
          position: "relative", overflow: "hidden",
          margin: "12px 0 12px 20px",
          borderRadius: 12,
          border: "1px solid rgba(100,150,255,0.1)",
        }}>
          <GridMap nodes={nodes} onNodeClick={setSelectedNode} selectedNode={selectedNode} />
          <div style={{
            position: "absolute", top: 12, left: 12, zIndex: 1000,
            background: "rgba(10,22,40,0.9)", borderRadius: 8, padding: "8px 12px",
            border: "1px solid rgba(100,150,255,0.15)",
            backdropFilter: "blur(8px)",
          }}>
            <div style={{ fontSize: 10, color: "#64748b", marginBottom: 4, fontWeight: 600, letterSpacing: "0.5px" }}>NETWORK STATUS</div>
            <div style={{ display: "flex", gap: 10, fontSize: 11 }}>
              <span style={{ display: "flex", alignItems: "center", gap: 4 }}>
                <CircleDot size={10} color="#10b981" /> {4 - criticalCount - warningCount} Normal
              </span>
              <span style={{ display: "flex", alignItems: "center", gap: 4 }}>
                <CircleDot size={10} color="#f59e0b" /> {warningCount} Warning
              </span>
              <span style={{ display: "flex", alignItems: "center", gap: 4 }}>
                <CircleDot size={10} color="#ef4444" /> {criticalCount} Critical
              </span>
            </div>
          </div>
        </div>

        {/* RIGHT SIDEBAR */}
        <div style={{
          display: "flex", flexDirection: "column", gap: 0,
          margin: "12px 20px 12px 12px", overflow: "hidden",
        }}>
          <div style={{
            flex: "0 0 auto",
            background: "rgba(15,23,42,0.4)",
            border: "1px solid rgba(100,150,255,0.1)",
            borderRadius: 12, overflow: "hidden",
            marginBottom: 12,
          }}>
            <div style={{
              padding: "10px 16px",
              borderBottom: "1px solid rgba(100,150,255,0.06)",
              display: "flex", alignItems: "center", gap: 6,
            }}>
              <Server size={14} color="#64a0ff" />
              <span style={{ fontSize: 12, fontWeight: 700, color: "#94a3b8", letterSpacing: "0.5px" }}>NODE DETAIL</span>
            </div>
            <NodeDetail node={selectedNodeData} weather={weather} />
          </div>

          <div style={{
            flex: 1,
            background: "rgba(15,23,42,0.4)",
            border: "1px solid rgba(100,150,255,0.1)",
            borderRadius: 12, overflow: "hidden",
            display: "flex", flexDirection: "column",
          }}>
            <div style={{
              padding: "10px 16px",
              borderBottom: "1px solid rgba(100,150,255,0.06)",
              display: "flex", alignItems: "center", gap: 6,
            }}>
              <Thermometer size={14} color="#ef4444" />
              <span style={{ fontSize: 12, fontWeight: 700, color: "#94a3b8", letterSpacing: "0.5px" }}>WEATHER CORRELATION</span>
            </div>
            <div style={{ flex: 1, overflow: "auto" }}>
              <WeatherCorrelation correlation={correlation} />
            </div>
          </div>
        </div>

        {/* BOTTOM SECTION */}
        <div style={{
          gridColumn: "1 / -1",
          display: "grid", gridTemplateColumns: "1fr 380px",
          gap: 12, padding: "0 20px 16px",
          maxHeight: 280,
        }}>
          <div style={{
            background: "rgba(15,23,42,0.4)",
            border: "1px solid rgba(100,150,255,0.1)",
            borderRadius: 12, overflow: "hidden",
            display: "flex", flexDirection: "column",
          }}>
            <div style={{
              padding: "10px 16px",
              borderBottom: "1px solid rgba(100,150,255,0.06)",
              display: "flex", alignItems: "center", gap: 6,
            }}>
              <AlertTriangle size={14} color="#f59e0b" />
              <span style={{ fontSize: 12, fontWeight: 700, color: "#94a3b8", letterSpacing: "0.5px" }}>ANOMALIES</span>
              {anomalies?.length > 0 && (
                <span style={{
                  padding: "1px 6px", borderRadius: 4, fontSize: 10,
                  background: "rgba(239,68,68,0.15)", color: "#fca5a5", marginLeft: 4,
                }}>{anomalies.length}</span>
              )}
            </div>
            <div style={{ flex: 1, overflow: "hidden" }}>
              <AnomalyTable anomalies={anomalies} />
            </div>
          </div>

          <div style={{
            background: "rgba(15,23,42,0.4)",
            border: "1px solid rgba(100,150,255,0.1)",
            borderRadius: 12, overflow: "hidden",
            display: "flex", flexDirection: "column",
          }}>
            <div style={{
              padding: "10px 16px",
              borderBottom: "1px solid rgba(100,150,255,0.06)",
              display: "flex", alignItems: "center", justifyContent: "space-between",
            }}>
              <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
                <BellRing size={14} color="#fbbf24" />
                <span style={{ fontSize: 12, fontWeight: 700, color: "#94a3b8", letterSpacing: "0.5px" }}>LIVE FEED</span>
              </div>
              {notifications?.length > 0 && (
                <span style={{
                  padding: "1px 8px", borderRadius: 10, fontSize: 10, fontWeight: 700,
                  background: "rgba(251,191,36,0.15)", color: "#fbbf24",
                }}>{notifications.length}</span>
              )}
            </div>
            <div style={{ flex: 1, overflow: "hidden" }}>
              <NotificationFeed notifications={notifications} />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default App;