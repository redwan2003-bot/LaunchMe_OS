# LaunchMe OS — Phone Link Android Companion Setup Guide

The **LaunchMe Phone Link Android Companion** allows you to monitor your AI agents (Claude Code, Antigravity, local agents) as living Mochi characters on your phone, and securely **Allow** or **Deny** bash/file commands directly from your mobile lock screen via end-to-end encrypted local P2P relay (AES-GCM 256-bit).

---

## 1. Installation on Android (Android 11 or higher)

1. Transfer `LaunchMe-PhoneLink-Companion-Android.apk` to your phone via USB, Google Drive, or Nearby Share.
2. Alternatively, if your phone is connected to your PC with USB debugging enabled, install via ADB:
   ```bash
   adb install LaunchMe-PhoneLink-Companion-Android.apk
   ```
3. Open the app on your phone. Grant notification permissions so approval alerts can wake your lock screen.

---

## 2. Pairing with LaunchMe OS (Windows 11)

1. In LaunchMe OS on Windows 11, open the **Dynamic Notch** or **Settings** panel.
2. Toggle **Phone Link** to **ON**.
3. LaunchMe OS will display a temporary **6-digit numeric PIN** and local UDP multicast beacon.
4. On your Android phone:
   - Tap **Pair New Computer** (or scan the pairing QR code / enter the 6-digit PIN).
5. Both devices will derive symmetric encryption keys using **PBKDF2 (100,000 iterations)**.
6. The connection is authenticated and encrypted via **AES-GCM 256-bit** with 96-bit random nonces and 128-bit authentication tags. Zero cloud servers or telemetry are involved.

---

## 3. Remote Features Available on Your Phone

- **Mochi Emotional Widget:** Real-time visual feedback of agent activity (`Working`, `Thinking`, `Approval Needed`, `Done`).
- **Lock-Screen Approval Sheet:** Biometric/PIN-protected **Allow** and **Deny** buttons with colorized git diff preview.
- **Dynamic Notch Island:** Floating status pill on Android over other apps while an agent runs on your PC.
- **Zero Battery Drain:** Communicates purely over local Wi-Fi UDP/TCP sockets when awake.
