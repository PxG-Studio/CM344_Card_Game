# Git/GitHub Troubleshooting Playbook

## Overview
This playbook provides step-by-step procedures for troubleshooting common Git and GitHub connection and push issues encountered during development.

## Quick Reference

### Common Error Messages
- `ERROR: user:132397714:HiroyasuDev` - GitHub authentication/permissions issue
- `fatal: Could not read from remote repository` - Connection or permissions problem
- `remote: Internal Server Error` - GitHub server-side issue
- `Connection refused` - Network/firewall blocking connection

## Troubleshooting Procedures

### 1. Verify Basic Connectivity

**Step 1: Test Internet Connection**
```bash
ping -c 3 github.com
```
- **Expected**: Successful ping responses
- **If fails**: Check network connection, firewall, or DNS

**Step 2: Test SSH Authentication**
```bash
ssh -T git@github.com
```
- **Expected**: "Hi [username]! You've successfully authenticated..."
- **If fails**: SSH key not set up or not added to GitHub account

**Step 3: Test HTTPS Connection**
```bash
curl -I https://github.com
```
- **Expected**: HTTP 200 or 301 response
- **If fails**: Network/firewall blocking HTTPS

### 2. Verify Git Remote Configuration

**Check Remote URL**
```bash
git remote -v
```

**Common Issues:**
- Wrong repository URL
- Using SSH when HTTPS is needed (or vice versa)
- Incorrect organization/repository name

**Fix:**
```bash
# Switch to SSH
git remote set-url origin git@github.com:Organization/Repository.git

# Switch to HTTPS
git remote set-url origin https://github.com/Organization/Repository.git
```

### 3. Verify SSH Key Setup

**Check if SSH keys are loaded**
```bash
ssh-add -l
```

**If no keys loaded:**
```bash
# Add your SSH key
ssh-add ~/.ssh/id_ed25519
# or
ssh-add ~/.ssh/id_rsa
```

**Verify SSH key is on GitHub:**
1. Go to GitHub → Settings → SSH and GPG keys
2. Ensure your public key is listed
3. If missing, add it using: `cat ~/.ssh/id_ed25519.pub`

### 4. Test Repository Access

**Check if you can read from repository**
```bash
git ls-remote --heads origin
```

**Expected**: List of remote branches
**If fails**: 
- Repository doesn't exist
- You don't have read access
- Network/firewall issue

### 5. Troubleshoot Push Failures

**Scenario: Push fails with authentication error**

**Step 1: Verify branch exists locally**
```bash
git branch --show-current
git log --oneline -3
```

**Step 2: Try explicit push**
```bash
git push origin <branch-name>
```

**Step 3: Try with verbose output**
```bash
GIT_SSH_COMMAND="ssh -v" git push origin <branch-name> 2>&1 | tail -20
```

**Step 4: Check if branch exists on remote**
```bash
git ls-remote --heads origin | grep <branch-name>
```

**If branch doesn't exist on remote:**
- Create branch on GitHub manually via web interface
- Then fetch and push: `git fetch origin && git push origin <branch-name>`

### 6. Network/Firewall Issues

**Symptoms:**
- Connection works for some operations but not others
- Intermittent failures
- "Connection refused" errors

**Solutions:**

**Option 1: Try SSH over HTTPS port (443)**
```bash
ssh -T -p 443 git@ssh.github.com
```

**Option 2: Check router/firewall settings**
- Ensure ports 22 (SSH) and 443 (HTTPS) are not blocked
- Check if VPN is interfering
- Try different network (hotspot, different WiFi)

**Option 3: Use HTTPS instead of SSH**
```bash
git remote set-url origin https://github.com/Organization/Repository.git
git push origin <branch-name>
```

### 7. Permission Issues

**Symptoms:**
- Authentication works but push fails
- "Could not read from remote repository" error
- Specific user ID in error message

**Check:**
1. Verify you have write access to the repository
2. Check organization settings (if applicable)
3. Verify branch protection rules aren't blocking pushes
4. Check if repository is private and your account has access

**For Organization Repositories:**
- Ensure your role has write permissions
- Check organization policies
- Verify SSH key has correct permissions

### 8. GitHub Server Issues

**Symptoms:**
- `remote: Internal Server Error`
- `500` HTTP errors
- Intermittent failures across all operations

**Check:**
1. Visit https://www.githubstatus.com/
2. Check GitHub's status page for ongoing incidents
3. Wait and retry after a few minutes

## Emergency Procedures

### Create Branch Manually on GitHub

If push continues to fail:

1. Go to repository on GitHub
2. Click branch dropdown
3. Type new branch name
4. Click "Create branch: <name> from <base-branch>"
5. Then locally:
   ```bash
   git fetch origin
   git checkout <branch-name>
   git push origin <branch-name>
   ```

### Force Push (Use with Caution)

**Only use if:**
- You're the only one working on the branch
- You understand the implications
- You've backed up your work

```bash
git push --force origin <branch-name>
```

## Prevention

### Best Practices

1. **Always verify connectivity before starting work**
   ```bash
   ssh -T git@github.com
   ```

2. **Keep SSH keys loaded**
   - Add to `~/.ssh/config` for automatic loading
   - Or use `ssh-add` in your shell profile

3. **Use HTTPS for initial clone if SSH fails**
   ```bash
   git clone https://github.com/Organization/Repository.git
   ```

4. **Regularly fetch to stay in sync**
   ```bash
   git fetch origin
   ```

5. **Check repository permissions periodically**
   - Especially after organization changes
   - After adding new SSH keys

## Escalation

If issues persist after following this playbook:

1. **Check GitHub Status**: https://www.githubstatus.com/
2. **Review GitHub Documentation**: https://docs.github.com/en/get-started
3. **Check Network**: Try different network connection
4. **Contact Repository Admin**: If organization permissions issue
5. **GitHub Support**: For persistent account/repository issues

## Related Documentation

- See `Technical-Manuals/Git-GitHub-Troubleshooting-Manual.md` for detailed technical information
- See `Setup-Guides/` for project-specific setup instructions

## Revision History

- **2025-11-18**: Initial creation based on develop-4 branch creation troubleshooting session

