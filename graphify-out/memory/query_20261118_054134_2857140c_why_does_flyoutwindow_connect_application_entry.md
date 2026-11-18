---
type: "query"
date: "2026-11-18T05:41:34.686842+00:00"
question: "Why does FlyoutWindow connect Application Entry & System Tray to Process Watcher & Freezer and System Modifier Lifecycle?"
contributor: "graphify"
outcome: "useful"
source_nodes: ["FlyoutWindow", "App", "IProcessFreezer", "ISessionCoordinator", "ISystemModifier"]
---

# Q: Why does FlyoutWindow connect Application Entry & System Tray to Process Watcher & Freezer and System Modifier Lifecycle?

## Answer

Expanded from original query via vocab: [flyout, window, session, coordinator, process, freezer, system, modifier, watcher, tray, app]. Traversed shortest paths and neighbor links in graphify-out/graph.json. FlyoutWindow has high betweenness centrality (0.170) because it serves as the single presentation convergence bridge in the desktop architecture. It holds constructor-injected interface contracts (ISessionCoordinator, IThermalThrottler, IWiFiWatchdog, IProcessFreezer) to display live telemetry and subscribe to state changes, while being instantiated and toggled by App in the System Tray.

## Outcome

- Signal: useful

## Source Nodes

- FlyoutWindow
- App
- IProcessFreezer
- ISessionCoordinator
- ISystemModifier