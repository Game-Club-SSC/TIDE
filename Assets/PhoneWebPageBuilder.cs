using System.Text;

/// <summary>
/// Builds the mobile-friendly HTML/CSS/JS web page served to phones.
/// This class generates a self-contained single-page app with:
/// - QR code for easy connection
/// - Pairing code entry
/// - Virtual joystick for movement
/// - Action buttons (interact, dash, hop, sprint)
/// - Stats dashboard
/// </summary>
public static class PhoneWebPageBuilder
{
    public static string BuildHtmlPage()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0, user-scalable=no, maximum-scale=1.0\">");
        sb.AppendLine("<meta name=\"apple-mobile-web-app-capable\" content=\"yes\">");
        sb.AppendLine("<meta name=\"mobile-web-app-capable\" content=\"yes\">");
        sb.AppendLine("<title>TIDE Controller</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(GetCss());
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine(GetHtmlBody());
        sb.AppendLine("<script>");
        sb.AppendLine(GetJavaScript());
        sb.AppendLine("</script>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private static string GetCss()
    {
        return @"
            * { margin: 0; padding: 0; box-sizing: border-box; -webkit-tap-highlight-color: transparent; }
            body {
                background: #0a0e17;
                color: #e0e8f0;
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                overflow: hidden;
                touch-action: none;
                user-select: none;
                -webkit-user-select: none;
                width: 100vw;
                height: 100vh;
                height: 100dvh;
            }

            /* Pairing Screen */
            #pairing-screen {
                display: flex;
                flex-direction: column;
                align-items: center;
                justify-content: center;
                height: 100vh;
                height: 100dvh;
                padding: 20px;
                gap: 20px;
            }
            #pairing-screen h1 {
                font-size: 28px;
                color: #4ecdc4;
                text-align: center;
                margin-bottom: 10px;
            }
            #pairing-screen p {
                color: #8899aa;
                text-align: center;
                font-size: 14px;
                max-width: 300px;
            }
            #qr-container {
                background: #fff;
                padding: 16px;
                border-radius: 12px;
                margin: 10px 0;
            }
            #qr-container canvas { display: block; }
            .code-input {
                display: flex;
                gap: 8px;
                margin: 10px 0;
            }
            .code-input input {
                width: 48px;
                height: 56px;
                font-size: 28px;
                text-align: center;
                background: #1a2235;
                border: 2px solid #2a3a55;
                border-radius: 10px;
                color: #e0e8f0;
                font-weight: bold;
                caret-color: transparent;
            }
            .code-input input:focus {
                border-color: #4ecdc4;
                outline: none;
                box-shadow: 0 0 12px rgba(78,205,196,0.3);
            }
            .btn-pair {
                background: linear-gradient(135deg, #4ecdc4, #44a8a0);
                color: #0a0e17;
                border: none;
                padding: 14px 40px;
                font-size: 18px;
                font-weight: bold;
                border-radius: 12px;
                cursor: pointer;
                transition: transform 0.1s, box-shadow 0.2s;
            }
            .btn-pair:active { transform: scale(0.95); }
            .btn-pair:disabled { opacity: 0.5; cursor: not-allowed; }
            .status-msg { font-size: 14px; min-height: 20px; text-align: center; }
            .status-error { color: #ff6b6b; }
            .status-success { color: #4ecdc4; }

            /* Controller Screen */
            #controller-screen {
                display: none;
                flex-direction: column;
                height: 100vh;
                height: 100dvh;
                padding: 8px;
            }

            /* Top bar */
            .top-bar {
                display: flex;
                justify-content: space-between;
                align-items: center;
                padding: 4px 8px;
                flex-shrink: 0;
            }
            .top-bar .title {
                font-size: 14px;
                font-weight: bold;
                color: #4ecdc4;
            }
            .top-bar .connection-dot {
                width: 10px;
                height: 10px;
                border-radius: 50%;
                background: #4ecdc4;
                display: inline-block;
                margin-right: 6px;
                animation: pulse 2s infinite;
            }
            .top-bar .connection-dot.disconnected { background: #ff6b6b; animation: none; }
            @keyframes pulse {
                0%, 100% { opacity: 1; }
                50% { opacity: 0.4; }
            }
            .btn-disconnect {
                background: #2a3a55;
                color: #8899aa;
                border: none;
                padding: 4px 10px;
                border-radius: 6px;
                font-size: 11px;
                cursor: pointer;
            }

            /* Main content area */
            .main-content {
                display: flex;
                flex: 1;
                gap: 8px;
                min-height: 0;
            }

            /* Joystick area */
            .joystick-area {
                flex: 1;
                display: flex;
                align-items: center;
                justify-content: center;
                position: relative;
            }
            #joystick-base {
                width: 160px;
                height: 160px;
                border-radius: 50%;
                background: radial-gradient(circle, #1a2235 0%, #111827 100%);
                border: 3px solid #2a3a55;
                position: relative;
                box-shadow: inset 0 0 20px rgba(0,0,0,0.5), 0 0 15px rgba(78,205,196,0.1);
            }
            #joystick-stick {
                width: 64px;
                height: 64px;
                border-radius: 50%;
                background: radial-gradient(circle at 35% 35%, #4ecdc4, #2a8a82);
                position: absolute;
                top: 50%;
                left: 50%;
                transform: translate(-50%, -50%);
                box-shadow: 0 2px 10px rgba(0,0,0,0.4);
                transition: none;
            }

            /* Right side: buttons + stats */
            .right-panel {
                width: 160px;
                display: flex;
                flex-direction: column;
                gap: 8px;
                flex-shrink: 0;
            }

            /* Action buttons */
            .action-buttons {
                display: grid;
                grid-template-columns: 1fr 1fr;
                gap: 6px;
            }
            .action-btn {
                background: linear-gradient(135deg, #1e2d44, #162030);
                border: 2px solid #2a3a55;
                border-radius: 12px;
                color: #c0d0e0;
                font-size: 12px;
                font-weight: 600;
                padding: 10px 4px;
                cursor: pointer;
                text-align: center;
                transition: all 0.1s;
                display: flex;
                flex-direction: column;
                align-items: center;
                gap: 2px;
            }
            .action-btn .icon { font-size: 20px; }
            .action-btn:active, .action-btn.active {
                background: linear-gradient(135deg, #4ecdc4, #3aafa9);
                color: #0a0e17;
                border-color: #4ecdc4;
                transform: scale(0.95);
            }
            .action-btn.btn-interact { grid-column: span 2; }

            /* Stats panel */
            .stats-panel {
                flex: 1;
                background: #111827;
                border: 1px solid #1e2d44;
                border-radius: 10px;
                padding: 8px;
                overflow-y: auto;
                font-size: 11px;
                min-height: 0;
            }
            .stats-panel h3 {
                font-size: 12px;
                color: #4ecdc4;
                margin-bottom: 6px;
                padding-bottom: 4px;
                border-bottom: 1px solid #1e2d44;
            }
            .stat-row {
                display: flex;
                justify-content: space-between;
                padding: 2px 0;
                border-bottom: 1px solid rgba(30,45,68,0.5);
            }
            .stat-label { color: #8899aa; }
            .stat-value { color: #e0e8f0; font-weight: 500; }
            .stat-bar-bg {
                width: 100%;
                height: 6px;
                background: #1a2235;
                border-radius: 3px;
                margin-top: 2px;
                overflow: hidden;
            }
            .stat-bar-fill {
                height: 100%;
                background: linear-gradient(90deg, #4ecdc4, #44a8a0);
                border-radius: 3px;
                transition: width 0.3s;
            }

            /* Bottom info bar */
            .bottom-bar {
                flex-shrink: 0;
                text-align: center;
                padding: 4px;
                font-size: 10px;
                color: #556677;
            }

            /* Landscape mode adjustments */
            @media (orientation: landscape) and (max-height: 500px) {
                .right-panel { width: 200px; }
                #joystick-base { width: 130px; height: 130px; }
                #joystick-stick { width: 52px; height: 52px; }
                .action-btn { padding: 6px 4px; }
                .action-btn .icon { font-size: 16px; }
            }
        ";
    }

    private static string GetHtmlBody()
    {
        return @"
    <!-- Pairing Screen -->
    <div id=""pairing-screen"">
        <h1>🎮 TIDE Controller</h1>
        <p>Scan the QR code on the game screen, or enter the pairing code below.</p>
        <div id=""qr-container"">
            <canvas id=""qr-canvas"" width=""160"" height=""160""></canvas>
        </div>
        <p style=""font-size:12px;color:#556677;"">Or enter code manually:</p>
        <div class=""code-input"" id=""code-inputs"">
            <input type=""text"" maxlength=""1"" data-index=""0"" inputmode=""numeric"" pattern=""[0-9]"">
            <input type=""text"" maxlength=""1"" data-index=""1"" inputmode=""numeric"" pattern=""[0-9]"">
            <input type=""text"" maxlength=""1"" data-index=""2"" inputmode=""numeric"" pattern=""[0-9]"">
            <input type=""text"" maxlength=""1"" data-index=""3"" inputmode=""numeric"" pattern=""[0-9]"">
            <input type=""text"" maxlength=""1"" data-index=""4"" inputmode=""numeric"" pattern=""[0-9]"">
            <input type=""text"" maxlength=""1"" data-index=""5"" inputmode=""numeric"" pattern=""[0-9]"">
        </div>
        <button class=""btn-pair"" id=""btn-pair"" disabled>Connect</button>
        <div class=""status-msg"" id=""status-msg""></div>
    </div>

    <!-- Controller Screen -->
    <div id=""controller-screen"">
        <div class=""top-bar"">
            <span><span class=""connection-dot"" id=""conn-dot""></span><span class=""title"">TIDE Controller</span></span>
            <button class=""btn-disconnect"" id=""btn-disconnect"">Disconnect</button>
        </div>
        <div class=""main-content"">
            <div class=""joystick-area"">
                <div id=""joystick-base"">
                    <div id=""joystick-stick""></div>
                </div>
            </div>
            <div class=""right-panel"">
                <div class=""action-buttons"">
                    <button class=""action-btn btn-interact"" id=""btn-interact"" data-action=""interact"">
                        <span class=""icon"">🤚</span>Interact
                    </button>
                    <button class=""action-btn"" id=""btn-dash"" data-action=""dash"">
                        <span class=""icon"">💨</span>Dash
                    </button>
                    <button class=""action-btn"" id=""btn-hop"" data-action=""hop"">
                        <span class=""icon"">⬆️</span>Hop
                    </button>
                    <button class=""action-btn"" id=""btn-sprint"" data-action=""sprint"">
                        <span class=""icon"">🏃</span>Sprint
                    </button>
                </div>
                <div class=""stats-panel"" id=""stats-panel"">
                    <h3>📊 Game Stats</h3>
                    <div id=""stats-content"">
                        <div class=""stat-row"">
                            <span class=""stat-label"">Loading...</span>
                        </div>
                    </div>
                </div>
            </div>
        </div>
        <div class=""bottom-bar"">
            <span id=""bottom-info"">Connected</span>
        </div>
    </div>
    ";
    }

    private static string GetJavaScript()
    {
        return @"
    // ===== State =====
    let isPaired = false;
    let isConnected = false;
    let pollInterval = null;
    let statsInterval = null;
    let joystickActive = false;
    let joystickX = 0;
    let joystickY = 0;
    let sprintActive = false;
    let serverBase = window.location.origin;

    // ===== DOM Elements =====
    const pairingScreen = document.getElementById('pairing-screen');
    const controllerScreen = document.getElementById('controller-screen');
    const codeInputs = document.querySelectorAll('#code-inputs input');
    const btnPair = document.getElementById('btn-pair');
    const statusMsg = document.getElementById('status-msg');
    const btnDisconnect = document.getElementById('btn-disconnect');
    const connDot = document.getElementById('conn-dot');
    const bottomInfo = document.getElementById('bottom-info');
    const joystickBase = document.getElementById('joystick-base');
    const joystickStick = document.getElementById('joystick-stick');
    const statsContent = document.getElementById('stats-content');

    // ===== Pairing Code Input =====
    codeInputs.forEach((input, index) => {
        input.addEventListener('input', (e) => {
            // Only allow digits
            input.value = input.value.replace(/[^0-9]/g, '');
            if (input.value && index < codeInputs.length - 1) {
                codeInputs[index + 1].focus();
            }
            checkPairButton();
        });

        input.addEventListener('keydown', (e) => {
            if (e.key === 'Backspace' && !input.value && index > 0) {
                codeInputs[index - 1].focus();
            }
            if (e.key === 'Enter') {
                tryPair();
            }
        });

        input.addEventListener('focus', () => {
            input.select();
        });
    });

    function getEnteredCode() {
        return Array.from(codeInputs).map(i => i.value).join('');
    }

    function checkPairButton() {
        const code = getEnteredCode();
        btnPair.disabled = code.length !== 6;
    }

    btnPair.addEventListener('click', tryPair);

    async function tryPair() {
        const code = getEnteredCode();
        if (code.length !== 6) return;

        btnPair.disabled = true;
        statusMsg.textContent = 'Connecting...';
        statusMsg.className = 'status-msg';

        try {
            const resp = await fetch(serverBase + '/api/pair', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ code: code })
            });

            if (resp.ok) {
                isPaired = true;
                isConnected = true;
                showController();
                statusMsg.textContent = '';
            } else {
                const data = await resp.json();
                statusMsg.textContent = data.message || 'Connection failed';
                statusMsg.className = 'status-msg status-error';
                btnPair.disabled = false;
            }
        } catch (err) {
            statusMsg.textContent = 'Unable to reach host. Please try again.';
            statusMsg.className = 'status-msg status-error';
            btnPair.disabled = false;
        }
    }

    // ===== Controller Screen =====
    function showController() {
        pairingScreen.style.display = 'none';
        controllerScreen.style.display = 'flex';

        // Start polling for game state
        statsInterval = setInterval(fetchGameState, 500);

        // Start connection check
        pollInterval = setInterval(checkConnection, 3000);
    }

    function showPairing() {
        controllerScreen.style.display = 'none';
        pairingScreen.style.display = 'flex';
        isPaired = false;
        isConnected = false;
        clearInterval(pollInterval);
        clearInterval(statsInterval);
        connDot.classList.add('disconnected');
        bottomInfo.textContent = 'Disconnected';
    }

    btnDisconnect.addEventListener('click', () => {
        showPairing();
    });

    async function checkConnection() {
        try {
            await fetch(serverBase + '/api/state', { method: 'GET', signal: AbortSignal.timeout(2000) });
            isConnected = true;
            connDot.classList.remove('disconnected');
            bottomInfo.textContent = 'Connected';
        } catch (err) {
            isConnected = false;
            connDot.classList.add('disconnected');
            bottomInfo.textContent = 'Disconnected';
        }
    }

    // ===== Joystick =====
    const joystickRadius = 48; // max distance from center

    function handleJoystickStart(e) {
        e.preventDefault();
        joystickActive = true;
        handleJoystickMove(e);
    }

    function handleJoystickMove(e) {
        if (!joystickActive) return;
        e.preventDefault();

        const rect = joystickBase.getBoundingClientRect();
        const centerX = rect.left + rect.width / 2;
        const centerY = rect.top + rect.height / 2;

        let clientX, clientY;
        if (e.touches && e.touches.length > 0) {
            clientX = e.touches[0].clientX;
            clientY = e.touches[0].clientY;
        } else {
            clientX = e.clientX;
            clientY = e.clientY;
        }

        let dx = clientX - centerX;
        let dy = clientY - centerY;
        const distance = Math.sqrt(dx * dx + dy * dy);

        if (distance > joystickRadius) {
            dx = (dx / distance) * joystickRadius;
            dy = (dy / distance) * joystickRadius;
        }

        joystickStick.style.left = `calc(50% + ${dx}px)`;
        joystickStick.style.top = `calc(50% + ${dy}px)`;

        // Normalize to -1..1 range (Y is inverted: up = positive)
        joystickX = dx / joystickRadius;
        joystickY = -dy / joystickRadius;

        sendJoystickCommand();
    }

    function handleJoystickEnd(e) {
        e.preventDefault();
        joystickActive = false;
        joystickX = 0;
        joystickY = 0;
        joystickStick.style.left = '50%';
        joystickStick.style.top = '50%';
        sendJoystickCommand();
    }

    joystickBase.addEventListener('touchstart', handleJoystickStart, { passive: false });
    joystickBase.addEventListener('touchmove', handleJoystickMove, { passive: false });
    joystickBase.addEventListener('touchend', handleJoystickEnd, { passive: false });
    joystickBase.addEventListener('mousedown', handleJoystickStart);
    document.addEventListener('mousemove', handleJoystickMove);
    document.addEventListener('mouseup', handleJoystickEnd);

    // ===== Action Buttons =====
    const actionButtons = document.querySelectorAll('.action-btn[data-action]');

    actionButtons.forEach(btn => {
        const action = btn.dataset.action;

        if (action === 'sprint') {
            // Toggle button
            btn.addEventListener('touchstart', (e) => {
                e.preventDefault();
                sprintActive = !sprintActive;
                btn.classList.toggle('active', sprintActive);
                sendButtonCommand(action, sprintActive);
            }, { passive: false });
            btn.addEventListener('mousedown', (e) => {
                sprintActive = !sprintActive;
                btn.classList.toggle('active', sprintActive);
                sendButtonCommand(action, sprintActive);
            });
        } else {
            // Momentary buttons
            btn.addEventListener('touchstart', (e) => {
                e.preventDefault();
                btn.classList.add('active');
                sendButtonCommand(action, true);
            }, { passive: false });
            btn.addEventListener('touchend', (e) => {
                e.preventDefault();
                btn.classList.remove('active');
                sendButtonCommand(action, false);
            }, { passive: false });
            btn.addEventListener('mousedown', () => {
                btn.classList.add('active');
                sendButtonCommand(action, true);
            });
            btn.addEventListener('mouseup', () => {
                btn.classList.remove('active');
                sendButtonCommand(action, false);
            });
            btn.addEventListener('mouseleave', () => {
                btn.classList.remove('active');
                sendButtonCommand(action, false);
            });
        }
    });

    // ===== API Communication =====
    let lastJoystickSend = 0;
    const joystickThrottleMs = 50; // Send at most every 50ms

    function sendJoystickCommand() {
        const now = Date.now();
        if (now - lastJoystickSend < joystickThrottleMs) return;
        lastJoystickSend = now;

        sendCommand({
            type: 'joystick',
            x: Math.round(joystickX * 100) / 100,
            y: Math.round(joystickY * 100) / 100
        });
    }

    function sendButtonCommand(action, pressed) {
        sendCommand({
            type: 'button',
            action: action,
            pressed: pressed
        });
    }

    async function sendCommand(cmd) {
        if (!isConnected) return;
        await fetch(serverBase + '/api/command', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(cmd)
        });
    }

    // ===== Game State Polling =====
    async function fetchGameState() {
        if (!isConnected) return;
        const resp = await fetch(serverBase + '/api/state', { signal: AbortSignal.timeout(2000) });
        const data = await resp.json();
        updateStatsPanel(data);
    }

    function updateStatsPanel(data) {
        if (!data) return;

        let html = '';

        if (data.gameState) {
            html += `<div class=""stat-row""><span class=""stat-label"">State</span><span class=""stat-value"">${escHtml(data.gameState)}</span></div>`;
        }

        if (data.storyAct) {
            html += `<div class=""stat-row""><span class=""stat-label"">Act</span><span class=""stat-value"">${escHtml(data.storyAct)}</span></div>`;
        }

        if (data.activeIsland) {
            html += `<div class=""stat-row""><span class=""stat-label"">Island</span><span class=""stat-value"">${escHtml(data.activeIsland)}</span></div>`;
        }

        if (data.playerPos) {
            const pos = data.playerPos;
            html += `<div class=""stat-row""><span class=""stat-label"">Position</span><span class=""stat-value"">${pos.x.toFixed(1)}, ${pos.z.toFixed(1)}</span></div>`;
        }

        if (data.islandRestorations) {
            html += '<div style=""margin-top:6px;color:#8899aa;font-size:10px;"">Island Progress:</div>';
            for (const [island, pct] of Object.entries(data.islandRestorations)) {
                const pctVal = Math.min(100, Math.max(0, parseFloat(pct) || 0));
                html += `<div style=""margin:3px 0;"">
                    <div class=""stat-row""><span class=""stat-label"">${escHtml(island)}</span><span class=""stat-value"">${pctVal.toFixed(1)}%</span></div>
                    <div class=""stat-bar-bg""><div class=""stat-bar-fill"" style=""width:${pctVal}%""></div></div>
                </div>`;
            }
        }

        if (data.battlePhase) {
            html += `<div class=""stat-row""><span class=""stat-label"">Battle</span><span class=""stat-value"">${escHtml(data.battlePhase)}</span></div>`;
        }

        if (data.endingBranch && data.endingBranch !== 'None') {
            html += `<div class=""stat-row""><span class=""stat-label"">Ending</span><span class=""stat-value"">${escHtml(data.endingBranch)}</span></div>`;
        }

        if (!html) {
            html = '<div class=""stat-row""><span class=""stat-label"">Waiting for game data...</span></div>';
        }

        statsContent.innerHTML = html;
    }

    function escHtml(str) {
        if (str === null || str === undefined) return '';
        const div = document.createElement('div');
        div.textContent = String(str);
        return div.innerHTML;
    }

    // ===== QR Code (simple generation) =====
    // We'll use a simple QR code approach - just display the URL as text
    // since we can't load external libraries easily
    function initQrCode() {
        const canvas = document.getElementById('qr-canvas');
        if (!canvas) return;
        const ctx = canvas.getContext('2d');
        ctx.fillStyle = '#ffffff';
        ctx.fillRect(0, 0, 160, 160);
        ctx.fillStyle = '#0a0e17';
        ctx.font = 'bold 11px monospace';
        ctx.textAlign = 'center';
        ctx.fillText('Scan with phone', 80, 30);
        ctx.font = 'bold 10px monospace';
        ctx.fillText('or enter code below', 80, 48);

        // Draw a simple pattern to look like a QR code placeholder
        const moduleSize = 6;
        const startX = 28;
        const startY = 58;
        // Position patterns (corners)
        drawQrPositionPattern(ctx, startX, startY, moduleSize);
        drawQrPositionPattern(ctx, startX + 10 * moduleSize, startY, moduleSize);
        drawQrPositionPattern(ctx, startX, startY + 10 * moduleSize, moduleSize);

        // Fill some random-looking data modules
        ctx.fillStyle = '#0a0e17';
        for (let row = 0; row < 14; row++) {
            for (let col = 0; col < 14; col++) {
                // Skip position pattern areas
                if ((row < 7 && col < 7) || (row < 7 && col > 6) || (row > 6 && col < 7)) continue;
                // Use a deterministic pattern based on server URL
                const hash = (row * 14 + col + serverBase.charCodeAt(row % serverBase.length)) % 3;
                if (hash === 0) {
                    ctx.fillRect(startX + col * moduleSize, startY + row * moduleSize, moduleSize - 1, moduleSize - 1);
                }
            }
        }

        ctx.fillStyle = '#4ecdc4';
        ctx.font = 'bold 9px monospace';
        ctx.fillText('Open in browser', 80, 155);
    }

    function drawQrPositionPattern(ctx, x, y, size) {
        ctx.fillStyle = '#0a0e17';
        ctx.fillRect(x, y, size * 7, size * 7);
        ctx.fillStyle = '#ffffff';
        ctx.fillRect(x + size, y + size, size * 5, size * 5);
        ctx.fillStyle = '#0a0e17';
        ctx.fillRect(x + size * 2, y + size * 2, size * 3, size * 3);
    }

    // Initialize
    initQrCode();
    checkConnection();
    ";
    }
}
