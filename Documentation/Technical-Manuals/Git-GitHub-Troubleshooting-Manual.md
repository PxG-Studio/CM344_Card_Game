# Git/GitHub Troubleshooting Technical Manual

## Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [Authentication Mechanisms](#authentication-mechanisms)
3. [Network Protocols](#network-protocols)
4. [Error Analysis](#error-analysis)
5. [Diagnostic Tools](#diagnostic-tools)
6. [Advanced Troubleshooting](#advanced-troubleshooting)

## Architecture Overview

### Git Remote Operations Flow

```
Local Git Client
    ↓
SSH/HTTPS Transport Layer
    ↓
Network (Firewall/Router/VPN)
    ↓
GitHub Servers
    ↓
Repository Access Control
    ↓
Git Operations (read/write)
```

### Key Components

1. **Local Git Client**: Handles repository operations
2. **Transport Layer**: SSH (port 22) or HTTPS (port 443)
3. **Network Infrastructure**: Routers, firewalls, VPNs
4. **GitHub Servers**: Host repositories and handle authentication
5. **Access Control**: Permissions, branch protection, organization policies

## Authentication Mechanisms

### SSH Authentication

**How It Works:**
1. Client generates SSH key pair (public/private)
2. Public key added to GitHub account
3. Private key stored locally (typically `~/.ssh/id_ed25519` or `~/.ssh/id_rsa`)
4. SSH agent manages key authentication

**Key Files:**
- `~/.ssh/id_ed25519` - Private key (ED25519 algorithm, recommended)
- `~/.ssh/id_rsa` - Private key (RSA algorithm, legacy)
- `~/.ssh/id_ed25519.pub` - Public key (to add to GitHub)
- `~/.ssh/config` - SSH configuration file
- `~/.ssh/known_hosts` - Known host fingerprints

**SSH Agent:**
```bash
# Check loaded keys
ssh-add -l

# Add key to agent
ssh-add ~/.ssh/id_ed25519

# List all keys (including unloaded)
ls -la ~/.ssh/id_*
```

**SSH Configuration:**
Create `~/.ssh/config` for automatic key selection:
```
Host github.com
    HostName github.com
    User git
    IdentityFile ~/.ssh/id_ed25519
    IdentitiesOnly yes
```

### HTTPS Authentication

**How It Works:**
1. Uses username/password or Personal Access Token (PAT)
2. Credentials stored in credential helper (osxkeychain on macOS)
3. Token-based authentication for better security

**Credential Helpers:**
- macOS: `osxkeychain`
- Linux: `cache` or `store`
- Windows: `wincred` or `manager`

**Personal Access Token:**
1. GitHub → Settings → Developer settings → Personal access tokens
2. Generate token with `repo` scope
3. Use token as password when prompted

## Network Protocols

### SSH Protocol (Port 22)

**Default Port:** 22
**Alternative Port:** 443 (via `ssh.github.com`)

**Connection Test:**
```bash
# Standard port
ssh -T git@github.com

# Alternative port (if 22 is blocked)
ssh -T -p 443 git@ssh.github.com
```

**Common Issues:**
- Port 22 blocked by firewall
- Corporate networks blocking SSH
- Router blocking outbound SSH

**Solutions:**
- Use port 443: `ssh -T -p 443 git@ssh.github.com`
- Configure SSH to use port 443 in `~/.ssh/config`
- Use HTTPS instead

### HTTPS Protocol (Port 443)

**Default Port:** 443

**Connection Test:**
```bash
curl -I https://github.com
```

**Advantages:**
- Usually not blocked by firewalls
- Works through most proxies
- Easier to debug

**Disadvantages:**
- Requires token management
- Slightly slower than SSH
- Token expiration issues

## Error Analysis

### Error: `ERROR: user:132397714:HiroyasuDev`

**Meaning:**
- GitHub is rejecting the operation
- Usually indicates permissions issue
- May be organization-level restriction

**Possible Causes:**
1. Repository permissions insufficient
2. Organization policy blocking operation
3. Branch protection rules
4. GitHub server-side issue

**Diagnosis:**
```bash
# Check repository access
git ls-remote --heads origin

# Check SSH authentication
ssh -T git@github.com

# Check if branch exists
git ls-remote --heads origin | grep <branch-name>
```

**Solutions:**
1. Verify repository permissions on GitHub
2. Check organization settings
3. Verify branch protection rules
4. Try creating branch manually on GitHub
5. Wait and retry (if server issue)

### Error: `fatal: Could not read from remote repository`

**Meaning:**
- Cannot establish connection to GitHub
- Authentication failed
- Repository doesn't exist or no access

**Possible Causes:**
1. Network connectivity issue
2. Firewall blocking connection
3. Wrong repository URL
4. Authentication failure
5. Repository doesn't exist

**Diagnosis:**
```bash
# Test network
ping github.com

# Test SSH
ssh -T git@github.com

# Test HTTPS
curl -I https://github.com

# Verify remote URL
git remote -v
```

**Solutions:**
1. Check network connection
2. Verify firewall settings
3. Check repository URL
4. Re-authenticate (SSH key or token)
5. Verify repository exists and you have access

### Error: `remote: Internal Server Error`

**Meaning:**
- GitHub server-side issue
- Temporary service disruption

**Solutions:**
1. Check GitHub status: https://www.githubstatus.com/
2. Wait a few minutes and retry
3. Try again later if issue persists

### Error: `Connection refused`

**Meaning:**
- Network cannot reach GitHub servers
- Firewall blocking connection
- DNS resolution issue

**Diagnosis:**
```bash
# Test DNS
nslookup github.com

# Test connectivity
ping github.com

# Test specific port
nc -zv github.com 22
nc -zv github.com 443
```

**Solutions:**
1. Check firewall rules
2. Verify DNS settings
3. Try different network
4. Use alternative port (443 for SSH)
5. Use HTTPS instead of SSH

## Diagnostic Tools

### Git Diagnostic Commands

**Check Git Configuration:**
```bash
git config --list
git config --get remote.origin.url
git config --get user.name
git config --get user.email
```

**Check Remote Status:**
```bash
git remote -v
git remote show origin
git ls-remote --heads origin
```

**Check Local Status:**
```bash
git status
git branch -vv
git log --oneline -5
```

**Verbose Push (Debug):**
```bash
GIT_SSH_COMMAND="ssh -v" git push origin <branch> 2>&1 | tail -30
GIT_CURL_VERBOSE=1 GIT_TRACE=1 git push origin <branch> 2>&1 | tail -30
```

### Network Diagnostic Commands

**Test Connectivity:**
```bash
# Ping test
ping -c 3 github.com

# DNS resolution
nslookup github.com
dig github.com

# Port connectivity
nc -zv github.com 22
nc -zv github.com 443
```

**Check Network Interfaces:**
```bash
# macOS/Linux
ifconfig
ip addr show

# Check default route
route get default
ip route show
```

**Check SSH Connection:**
```bash
# Standard test
ssh -T git@github.com

# Verbose test
ssh -vT git@github.com

# Test alternative port
ssh -T -p 443 git@ssh.github.com
```

### System Diagnostic Commands

**Check SSH Keys:**
```bash
# List loaded keys
ssh-add -l

# List all keys
ls -la ~/.ssh/

# Test key
ssh-keygen -l -f ~/.ssh/id_ed25519.pub
```

**Check Credential Helper:**
```bash
# macOS
git config --global credential.helper
security find-internet-password -s github.com

# List all credentials
git credential-osxkeychain get
```

## Advanced Troubleshooting

### SSH Key Issues

**Problem: Key not recognized by GitHub**

**Solution:**
1. Verify public key is on GitHub:
   ```bash
   cat ~/.ssh/id_ed25519.pub
   ```
2. Compare with GitHub → Settings → SSH and GPG keys
3. If different, add correct key to GitHub

**Problem: Multiple SSH keys**

**Solution:**
Create `~/.ssh/config`:
```
Host github.com
    HostName github.com
    User git
    IdentityFile ~/.ssh/id_ed25519
    IdentitiesOnly yes
```

### Repository Access Issues

**Problem: Can read but cannot write**

**Diagnosis:**
- `git fetch` works but `git push` fails
- Authentication successful but operation denied

**Solutions:**
1. Verify write permissions on repository
2. Check organization role (if applicable)
3. Verify branch protection rules
4. Check if repository is archived

### Network Configuration Issues

**Problem: Works on one network but not another**

**Diagnosis:**
- Different behavior on different networks
- Suggests network-specific blocking

**Solutions:**
1. Check firewall rules on problematic network
2. Use HTTPS instead of SSH
3. Use SSH over port 443
4. Configure VPN to allow Git operations

### Organization Repository Issues

**Problem: Personal repos work but org repos don't**

**Diagnosis:**
- Organization-specific policies
- Different permission model

**Solutions:**
1. Verify organization membership
2. Check organization repository permissions
3. Verify organization policies
4. Contact organization admin

## Best Practices

### SSH Key Management

1. **Use ED25519 keys** (more secure, faster)
   ```bash
   ssh-keygen -t ed25519 -C "your_email@example.com"
   ```

2. **Use SSH agent** to avoid entering passphrase repeatedly
   ```bash
   eval "$(ssh-agent -s)"
   ssh-add ~/.ssh/id_ed25519
   ```

3. **Use SSH config** for automatic key selection
   ```bash
   # ~/.ssh/config
   Host github.com
       HostName github.com
       User git
       IdentityFile ~/.ssh/id_ed25519
   ```

### Repository Management

1. **Always verify remote URL** before pushing
   ```bash
   git remote -v
   ```

2. **Fetch before push** to avoid conflicts
   ```bash
   git fetch origin
   git push origin <branch>
   ```

3. **Use descriptive branch names** following conventions

4. **Regularly sync** with remote
   ```bash
   git fetch origin
   git pull origin <branch>
   ```

### Network Configuration

1. **Test connectivity** before starting work
   ```bash
   ssh -T git@github.com
   ```

2. **Have fallback options** (HTTPS if SSH fails)
   ```bash
   git remote set-url origin https://github.com/org/repo.git
   ```

3. **Document network requirements** for team

## Troubleshooting Checklist

Use this checklist when troubleshooting Git/GitHub issues:

- [ ] Internet connectivity working (ping test)
- [ ] GitHub accessible (curl/SSH test)
- [ ] SSH authentication working (`ssh -T git@github.com`)
- [ ] SSH keys loaded in agent (`ssh-add -l`)
- [ ] Remote URL correct (`git remote -v`)
- [ ] Repository exists and accessible (`git ls-remote`)
- [ ] Branch exists locally (`git branch`)
- [ ] Branch exists on remote (`git ls-remote --heads`)
- [ ] Permissions sufficient (can read/write)
- [ ] No branch protection blocking push
- [ ] GitHub status page shows no incidents
- [ ] Network not blocking ports 22/443
- [ ] Credential helper configured (for HTTPS)

## Related Documentation

- **Playbook**: `Playbooks/Git-GitHub-Troubleshooting-Playbook.md` - Step-by-step procedures
- **GitHub Docs**: https://docs.github.com/en/get-started
- **SSH Documentation**: https://docs.github.com/en/authentication/connecting-to-github-with-ssh

## Revision History

- **2025-11-18**: Initial creation based on develop-4 branch creation troubleshooting session
- Includes analysis of authentication, network protocols, and error patterns

