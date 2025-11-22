# MCP Unity Server - Cursor IDE Configuration Guide

This guide will help you configure Cursor IDE to connect to the MCP Unity Server, enabling AI-assisted Unity development.

## Prerequisites

- ✅ Unity Editor with MCP Unity Server package installed (already done)
- ✅ MCP Unity Server configured on port 8090 (already configured)
- ✅ Cursor IDE installed

## Current Unity Configuration

Your Unity project is already configured with:
- **Port**: 8090
- **Auto Start Server**: Enabled
- **Request Timeout**: 10 seconds
- **Info Logs**: Enabled

Configuration file: `ProjectSettings/McpUnitySettings.json`

## Cursor IDE Configuration Steps

### Step 1: Open Cursor Settings

1. Open Cursor IDE
2. Press `Ctrl+,` (Windows/Linux) or `Cmd+,` (Mac) to open Settings
3. Or go to: **File → Preferences → Settings**

### Step 2: Configure MCP Servers

1. In the Settings search bar, type: `mcp` or `model context protocol`
2. Look for **"MCP Servers"** or **"Model Context Protocol"** settings
3. Click **"Edit in settings.json"** or find the MCP configuration section

### Step 3: Add MCP Unity Server Configuration

Add the following configuration to your Cursor settings:

```json
{
  "mcp.servers": {
    "unity": {
      "command": "node",
      "args": [
        "path/to/mcp-unity/server.js"
      ],
      "env": {
        "UNITY_PORT": "8090"
      }
    }
  }
}
```

**OR** if Cursor uses a different format, try:

```json
{
  "mcp": {
    "servers": {
      "unity": {
        "url": "http://localhost:8090",
        "type": "stdio"
      }
    }
  }
}
```

### Step 4: Alternative - Direct Connection Method

If the above doesn't work, you may need to:

1. **Find the MCP Unity Server executable/script**:
   - Check: `Packages/com.gamelovers.mcp-unity/`
   - Look for a server script or executable

2. **Configure as stdio server**:
   ```json
   {
     "mcp.servers": {
       "unity": {
         "command": "path/to/unity/mcp/server",
         "args": ["--port", "8090"]
       }
     }
   }
   ```

### Step 5: Verify Connection

1. Make sure Unity Editor is running
2. The MCP Unity Server should auto-start (check Unity Console for confirmation)
3. In Cursor, try using MCP-related commands or check the MCP status

## Troubleshooting

### Server Not Starting
- Check Unity Console for MCP server logs
- Verify port 8090 is not in use by another application
- Check `ProjectSettings/McpUnitySettings.json` for correct configuration

### Cursor Not Connecting
- Verify Unity Editor is running
- Check if MCP server is actually running (look for logs in Unity Console)
- Try restarting both Unity and Cursor
- Check Cursor's developer console for connection errors

### Port Conflicts
If port 8090 is in use, you can change it in:
- `ProjectSettings/McpUnitySettings.json` → Change `"Port": 8090` to another port
- Update Cursor configuration to match the new port

## Testing the Connection

Once configured, you should be able to:
- Ask Cursor to execute Unity menu items
- Request Unity console logs
- Get information about Unity GameObjects
- Create/modify Unity assets through Cursor

## Additional Resources

- MCP Unity Server GitHub: https://github.com/CoderGamester/mcp-unity
- Cursor IDE Documentation: Check Cursor's official docs for MCP configuration

## Current Settings Reference

Your current `McpUnitySettings.json`:
```json
{
    "Port": 8090,
    "RequestTimeoutSeconds": 10,
    "AutoStartServer": true,
    "EnableInfoLogs": true,
    "NpmExecutablePath": "",
    "AllowRemoteConnections": false
}
```

---

**Note**: The exact configuration format may vary depending on your Cursor version. If the above doesn't work, check Cursor's latest documentation for MCP server configuration.

