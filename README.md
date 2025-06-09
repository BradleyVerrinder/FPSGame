# 🕹️ Multiplayer FPS (Unity + Netcode)

A real-time multiplayer first-person shooter built using Unity and C#, featuring synced movement, shooting, animations, and a server-authoritative architecture using Unity Netcode and Relay.

## 🎯 Project Overview

This project is a networked FPS game prototype where players can connect via Unity's Relay service and compete in the same virtual space. It was developed as a hands-on exploration of distributed systems in gaming, and includes core features essential to modern online shooters.

## ✨ Features

- ✅ **Multiplayer Connectivity**
  - Unity Relay integration for internet-based connections
  - Host/client flow for joining matches

- 🎮 **Player Mechanics**
  - Smooth first-person movement and mouse look
  - Jumping and shooting mechanics
  - Death and respawn cycle

- 🧠 **Server-Authoritative Design**
  - Server is the source of truth for movement, damage, and deaths
  - Prevents client-side cheating and desync

- ⚙️ **Networking Enhancements**
  - **Client-side prediction**: Player movement feels responsive even with latency
  - **Interpolation**: Remote players move smoothly, reducing jitter
  - All key actions synced across all clients

- 🖥️ **User Interface**
  - HUD displays health and respawn timer
  - Feedback for damage and deaths

## 🧱 Technologies Used

- **Unity** (v2023+)
- **C#**
- **Unity Netcode for GameObjects (NGO)**
- **Unity Relay (via Unity Transport)**
- **Netcode utilities** for prediction/interpolation

## 🔮 Future Improvements

- Mounting Surfaces
- Throwables such as Grenades, Smokes etc
- Users can choose between a selection of maps
- Recoil Patterns based on the chosen Gun
