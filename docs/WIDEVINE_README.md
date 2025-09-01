# Widevine Key Dumping Guide

This guide provides step-by-step instructions to dump Widevine keys using an Android emulator, `keydive`, and Frida tools. Follow the instructions carefully to set up the environment and extract Widevine keys successfully.
These keys are required to use AppleMusic Downloader tool. When you finish, you'll also get two desired files - `client_id.bin` and `private_key.pem` which you'll use in this tool.

## Prerequisites

- **Android Studio**: Download and install from [developer.android.com](https://developer.android.com/studio).
- **Python 3**: Version 3.13 or higher recommended. Download from [python.org](https://www.python.org/downloads/).
- **ADB (Android Debug Bridge)**: Included in Android Studio's SDK platform-tools.
- A compatible system with administrative privileges to modify environment variables.

## Setup Instructions

### 1. Install Android Studio and SDK
1. Install **Android Studio** if not already installed.
2. Open Android Studio and navigate to **File > Settings > Languages & Frameworks > Android SDK**.
3. Select **Android 14 (API Level 34)** under the **SDK Platforms** tab.
4. Click **Apply** to install the SDK to `%localappdata%\Android\Sdk\`.

### 2. Create an Android Emulator
1. In Android Studio, open the **Device Manager**.
2. Click **Create Virtual Device** and select **Pixel 4 XL**.
3. Choose **API 34** with the **Google APIs Intel x86_64 Atom System Image**.
4. Confirm and create the virtual device.

### 3. Install Python and Required Tools
1. Install **Python 3** (version 3.13 recommended).
2. Open a Command Prompt and install `frida-tools` and `keydive`:
   ```bash
   pip install frida frida-tools keydive
   ```
3. Check the installed Frida version:
   ```bash
   frida --version
   ```
4. Download the corresponding `frida-server` version (e.g., `frida-server-17.2.4-android-x86_64.xz`) from [Frida releases](https://github.com/frida/frida/releases).
5. Extract the `frida-server` binary from the archive.

### 4. Configure ADB
1. Locate the Android SDK's `platform-tools` folder (e.g., `%localappdata%\Android\Sdk\platform-tools`).
2. Add this folder to your system's **Path** environment variable:
   - Open **System > Advanced system settings > Environment Variables**.
   - Under **User** or **System variables**, select **Path**, click **Edit**, and add the `platform-tools` folder path.
3. Reboot your system to apply the changes.
4. Verify ADB installation by running:
   ```bash
   adb --version
   ```

### 5. Set Up Frida Server on the Emulator
1. Start the emulator in Android Studio.
2. Verify the emulator is detected:
   ```bash
   adb devices
   ```
3. Root the emulator and push the `frida-server` binary:
   ```bash
   adb root
   adb push frida-server-17.2.4-android-x86_64 /sdcard
   ```
4. Access the emulator shell and configure `frida-server`:
   ```bash
   adb shell
   mv /sdcard/frida-server-17.2.4-android-x86_64 /data/local/tmp/frida-server
   chmod 755 /data/local/tmp/frida-server
   /data/local/tmp/frida-server &
   ```

### 6. Run Keydive
1. Open a new Command Prompt in the folder where you want to save the output keys.
2. Run the `keydive` command:
   ```bash
   keydive -kw -a player
   ```
3. On the emulator, observe the app opening with a pink icon in the bottom corner.
4. Click the pink icon and select **Provision Widevine**.

### 7. Verify Output
- If successful, you should see logs similar to:
  ```
  2025-07-26 08:24:58 [I] keydive: Version: 3.0.5
  2025-07-26 08:24:58 [I] Remote: Connected to device: Android Emulator 5556 (emulator-5556)
  2025-07-26 08:24:58 [I] Remote: SDK API: 34
  2025-07-26 08:24:58 [I] Remote: ABI CPU: x86_64
  2025-07-26 08:24:58 [I] Core: Preparing DRM player: Kaltura Device Info (com.kaltura.kalturadeviceinfo)
  2025-07-26 08:25:00 [I] Core: Starting application: Kaltura Device Info (com.kaltura.kalturadeviceinfo)
  2025-07-26 08:25:01 [I] Core: Watcher delay: 1.0s
  2025-07-26 08:25:01 [I] Core: Detected process: 420 (android.hardware.drm-service.widevine)
  2025-07-26 08:25:02 [I] Core: Library found: android.hardware.drm-service.widevine (/apex/com.google.android.widevine/bin/hw/android.hardware.drm-service.widevine)
  2025-07-26 08:25:02 [I] Core: Successfully attached hook to process: 420
  2025-07-26 08:25:07 [I] Cdm: Received encrypted keybox:
  ```
- The extracted Widevine keys will be saved in the folder where you ran the `keydive` command.

## Notes
- The output keys are saved relative to the current working directory when running `keydive`.
- Copy the keys as needed for your use case.
- Ensure the emulator remains powered on during the process.
- If you encounter issues, verify the Frida server version matches the installed `frida-tools` version.

## Troubleshooting
- **ADB not recognized**: Ensure the `platform-tools` path is correctly added to your environment variables and the system is rebooted.
- **Frida-server fails to run**: Confirm the server binary matches your Frida version and has the correct permissions (`chmod 755`).
- **Keydive errors**: Ensure the emulator is running API 34 with the Google APIs Intel x86_64 image.

## License
This guide is provided for educational purposes only. Ensure compliance with applicable laws and terms of service when using Widevine keys.
