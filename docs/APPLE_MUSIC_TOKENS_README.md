# Apple Music Tokens Guide

This guide provides clear instructions for obtaining the **API Token** and **Media Token** required to use an Apple Music Downloader. Follow these steps carefully to retrieve the necessary tokens from the Apple Music website.

## Prerequisites

- An active **Apple Music subscription** tied to your Apple account.
- A web browser with developer tools (e.g., Chrome, Firefox, Safari).
- Access to [music.apple.com](https://music.apple.com).

> **Note**: Tokens may expire, so you may need to repeat these steps periodically to obtain fresh tokens.

## Step-by-Step Instructions

### 1. Obtain the API Token
The API Token is required to authenticate requests to Apple Music's API.

1. Open your web browser and navigate to [music.apple.com](https://music.apple.com).
2. Open the browser's **Developer Tools**:
   - **Chrome**: Right-click on the page, select **Inspect**, and go to the **Console** tab.
   - **Firefox**: Right-click, select **Inspect**, and go to the **Console** tab.
   - **Safari**: Enable Developer Tools in **Preferences > Advanced**, then right-click, select **Inspect Element**, and go to the **Console** tab.
3. In the Console, paste and execute the following JavaScript code:
   ```javascript
   MusicKit.getInstance().developerToken
   ```
4. Copy the output token string. This is your **API Token**.

### 2. Obtain the Media Token
The Media Token is tied to your Apple Music account and authenticates your user session.

1. Go to [music.apple.com](https://music.apple.com) and **log in** to your Apple account with an active Apple Music subscription.
2. Open the browser's **Developer Tools** (as described above).
3. Navigate to the **Application** tab (in Chrome) or **Storage** tab (in Firefox/Safari).
4. Under **Cookies**, locate the cookie named `media-user-token`.
5. Copy the value of the `media-user-token` cookie. This is your **Media Token**.

## Important Notes
- **Subscription Requirement**: You must have an active Apple Music subscription to generate valid tokens.
- **Token Expiry**: Both the API Token and Media Token can expire. If you encounter issues with the downloader, regenerate the tokens by repeating the steps above.
- **Security**: Keep your tokens secure and do not share them publicly, as they are tied to your Apple Music account.

## Troubleshooting
- **No API Token output**: Ensure you are on [music.apple.com](https://music.apple.com) and that the MusicKit JavaScript library is loaded. Try refreshing the page and re-running the command.
- **No Media Token found**: Verify that you are logged in with an active Apple Music subscription. Check the cookies section in Developer Tools again.
- **Downloader errors**: Confirm that both tokens are valid and not expired. Regenerate them if necessary.

## Usage
Use the retrieved **API Token** and **Media Token** in your Apple Music downloader as required by its configuration. Refer to the downloader's documentation for specific instructions on where to input these tokens.

## License
This guide is provided for educational purposes only. Ensure compliance with Apple Music's terms of service and applicable laws when using these tokens.
