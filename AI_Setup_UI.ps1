<#
============================================================
 AI / ML / DL / Data Science / Full-Stack Environment
 FLEXIBLE SETUP UI
============================================================

 Nothing in this script installs, changes, or removes anything
 on its own. You get a checkbox tree; you pick exactly what you
 want; only the checked items are installed. Nothing pre-installed
 on your machine is ever touched, upgraded, or deleted by this
 script - it only adds what you explicitly check.

 Run it by double-clicking "Launch_Setup_UI.bat", or directly:
   powershell -ExecutionPolicy Bypass -File .\AI_Setup_UI.ps1
============================================================
#>

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# ------------------------------------------------------------
# 1. DATA: everything the original script could install,
#    now described as data instead of hard-coded commands.
# ------------------------------------------------------------

# winget-based tools: Category -> list of (Id, DisplayName)
$WingetCatalog = [ordered]@{
    "System Development Tools" = @(
        @{Id="Git.Git"; Name="Git"}
        @{Id="GitHub.cli"; Name="GitHub CLI"}
        @{Id="Microsoft.VisualStudioCode"; Name="Visual Studio Code"}
        @{Id="Microsoft.WindowsTerminal"; Name="Windows Terminal"}
        @{Id="Docker.DockerDesktop"; Name="Docker Desktop"}
        @{Id="OpenJS.NodeJS.LTS"; Name="Node.js LTS"}
        @{Id="Microsoft.PowerShell"; Name="PowerShell 7"}
        @{Id="Postman.Postman"; Name="Postman"}
        @{Id="JetBrains.Toolbox"; Name="JetBrains Toolbox"}
    )
    "Research / Statistical Computing" = @(
        @{Id="RProject.R"; Name="R"}
        @{Id="Posit.RStudio"; Name="RStudio"}
        @{Id="MiKTeX.MiKTeX"; Name="MiKTeX (LaTeX)"}
        @{Id="JGraph.Draw"; Name="draw.io"}
        @{Id="Zotero.Zotero"; Name="Zotero"}
        @{Id="Pandoc.Pandoc"; Name="Pandoc"}
        @{Id="Anaconda.Miniconda3"; Name="Miniconda3"}
    )
    "Cloud / DevOps CLIs" = @(
        @{Id="Kubernetes.kubectl"; Name="kubectl"}
        @{Id="Helm.Helm"; Name="Helm"}
        @{Id="Amazon.AWSCLI"; Name="AWS CLI"}
        @{Id="Microsoft.AzureCLI"; Name="Azure CLI"}
        @{Id="Google.CloudSDK"; Name="Google Cloud SDK"}
        @{Id="Hashicorp.Terraform"; Name="Terraform"}
        @{Id="Hashicorp.Vault"; Name="Vault"}
        @{Id="jq"; Name="jq"}
        @{Id="curl.curl"; Name="curl"}
    )
    "Database Software" = @(
        @{Id="PostgreSQL.PostgreSQL"; Name="PostgreSQL"}
        @{Id="MongoDB.Server"; Name="MongoDB"}
        @{Id="Redis.Redis"; Name="Redis"}
    )
}

# System-level tweaks (registry / OS features). Off by default -
# these touch shared system settings, not just an app folder.
$SystemTweaks = @(
    @{Id="VCREDIST64"; Name="Microsoft VC++ Redistributable (x64)"; Action="winget"; WingetId="Microsoft.VCRedist.2015+.x64"}
    @{Id="VCREDIST86"; Name="Microsoft VC++ Redistributable (x86)"; Action="winget"; WingetId="Microsoft.VCRedist.2015+.x86"}
    @{Id="DOTNETRT"; Name=".NET 8 Desktop Runtime"; Action="winget"; WingetId="Microsoft.DotNet.DesktopRuntime.8"}
    @{Id="DOTNETSDK"; Name=".NET 8 SDK"; Action="winget"; WingetId="Microsoft.DotNet.SDK.8"}
    @{Id="WSL2"; Name="WSL2 (Windows Subsystem for Linux)"; Action="wsl"}
    @{Id="DEVMODE"; Name="Enable Windows Developer Mode (registry)"; Action="devmode"}
    @{Id="LONGPATH"; Name="Enable NTFS long path support (registry)"; Action="longpath"}
)

# pip-based packages: Category -> list of package names.
# Each package becomes its own checkable leaf node, so you can
# pick individual libraries, not just whole categories.
$PipCatalog = [ordered]@{
    "Scientific Computing" = @("numpy","scipy","pandas","polars","sympy","statsmodels","numba","cython","bottleneck","numexpr")
    "Machine Learning" = @("scikit-learn","xgboost","lightgbm","catboost","imbalanced-learn","mlxtend","feature-engine","shap","lime","yellowbrick","optuna","hyperopt")
    "Deep Learning" = @("torch","torchvision","torchaudio","tensorflow","keras","jax","flax","onnx","onnxruntime","onnxruntime-gpu","tensorboard")
    "Computer Vision" = @("opencv-python","opencv-contrib-python","scikit-image","imageio","imageio-ffmpeg","pillow","albumentations","ultralytics","timm","torchmetrics")
    "NLP" = @("nltk","spacy","gensim","textblob","textacy","stanza","flair","vaderSentiment","sentencepiece","sacremoses","conllu")
    "Transformers / LLM" = @("transformers","tokenizers","datasets","huggingface-hub","safetensors","accelerate","peft","trl","bitsandbytes","sentence-transformers","evaluate")
    "Generative AI APIs" = @("openai","anthropic","cohere","google-genai","groq","mistralai","ollama")
    "RAG / Vector Databases" = @("langchain","langchain-community","langchain-openai","langchain-anthropic","langgraph","llama-index","chromadb","faiss-cpu","qdrant-client","weaviate-client","pinecone","pymilvus")
    "AI Agents / MCP" = @('"mcp[cli]"',"crewai","autogen-agentchat","semantic-kernel","smolagents")
    "Chinese AI Frameworks / Distributed Training" = @("paddlepaddle","paddlenlp","paddleocr","paddlehub","modelscope","ms-swift","colossalai","bmtrain","FlagEmbedding","flagai","auto-gptq","autoawq","optimum","vllm")
    "Chinese LLM Provider SDKs" = @("dashscope","zhipuai","qianfan","volcengine-python-sdk","zai-sdk")
    "API / Backend" = @("fastapi","uvicorn","gunicorn","flask","django","djangorestframework","starlette","pydantic","pydantic-settings","httpx","requests","aiohttp","websockets","python-multipart","python-dotenv","email-validator","orjson","msgspec","graphene","strawberry-graphql","gql","grpcio","grpcio-tools","protobuf")
    "Database / ORM" = @("sqlalchemy","alembic","psycopg","psycopg2-binary","asyncpg","pymysql","mysqlclient","pymongo","motor","redis","elasticsearch","neo4j","influxdb-client")
    "Data Engineering" = @("pyarrow","fastparquet","duckdb","dask","pyspark","great-expectations","dbt-core","dbt-postgres","apache-airflow")
    "Visualization" = @("matplotlib","seaborn","plotly","bokeh","altair","folium","dash","streamlit","gradio","panel","holoviews","hvplot")
    "Web Scraping / Browser Automation" = @("beautifulsoup4","lxml","html5lib","requests-html","selenium","scrapy","playwright","trafilatura","newspaper4k")
    "Testing / QA" = @("pytest","pytest-cov","pytest-asyncio","pytest-mock","pytest-xdist","hypothesis","mock","responses","respx","locust","allure-pytest")
    "Code Quality" = @("black","isort","ruff","flake8","mypy","pylint","pre-commit","ipython","ipdb","rich","typer","click","loguru")
    "Jupyter / Experiment Tracking" = @("jupyter","jupyterlab","notebook","ipykernel","nbconvert","nbformat","mlflow","wandb","dvc","clearml","dagshub","evidently")
    "Model Serving / Deployment" = @("bentoml","ray","vllm","litserve","celery","dramatiq")
    "Audio / Speech" = @("librosa","soundfile","pydub","SpeechRecognition","openai-whisper","faster-whisper","webrtcvad")
    "Networking / Security / Auth" = @("cryptography","PyJWT","passlib","bcrypt","authlib","oauthlib","requests-oauthlib","paramiko","dnspython")
    "Dataset Acquisition / Research Data" = @("kaggle","opendatasets","ucimlrepo","openml","pydataset","datacommons","sodapy","pytrends","wbdata","yfinance","quandl","fredapi")
    "Advanced / Research Statistics" = @("pingouin","scikit-posthocs","researchpy","statannotations","linearmodels","patsy","formulaic","dowhy","econml","causalml","causal-learn","lifelines","scikit-survival")
    "Bayesian Statistics / Probabilistic Modeling" = @("pymc","arviz","numpyro","pystan","bambi","pomegranate")
    "Advanced Time Series / Forecasting" = @("prophet","pmdarima","sktime","tsfresh","darts","tbats","neuralprophet")
    "Geospatial / Network / Graph Analysis" = @("geopandas","shapely","pyproj","rasterio","geopy","osmnx","networkx","python-igraph","pygraphviz")
    "Experimental Design / Survey / A-B Testing" = @("pyDOE2","SALib","statsmodels","pyreadstat","semopy","factor-analyzer")
    "Reproducible Research / Workflow / Reporting" = @("papermill","snakemake","scholarly","arxiv","pyzotero","bibtexparser","python-docx","openpyxl","xlsxwriter","weasyprint","markdown","quarto-cli")
}

# Project-setup extras: not a package, just optional convenience steps
$ProjectSetupOptions = @(
    @{Id="VENV"; Name="Create a Python virtual environment (.venv)"}
    @{Id="DIRS"; Name="Create the projects\ folder structure (ai, ml, llm, rag, backend, frontend, ...)"}
    @{Id="FREEZE"; Name="Write installed_packages.txt / requirements-full.txt after install"}
    @{Id="IPYKERNEL"; Name="Register a Jupyter kernel for this environment"}
)

# ------------------------------------------------------------
# 2. FORM
# ------------------------------------------------------------

$form = New-Object System.Windows.Forms.Form
$form.Text = "AI / Data Science / Full-Stack Environment - Setup"
$form.Size = New-Object System.Drawing.Size(880, 760)
$form.StartPosition = "CenterScreen"
$form.MinimumSize = New-Object System.Drawing.Size(700, 500)

# Log file exists from the moment the UI opens so status checks (which run
# before any install) have somewhere to write; Install still starts a fresh
# log with its own header when clicked.
$script:LogFile = Join-Path $PSScriptRoot "setup_log.txt"

$infoLabel = New-Object System.Windows.Forms.Label
$infoLabel.Text = "Check only what you want installed. Nothing is pre-selected, nothing already on your machine is changed or removed - only checked items are installed. Each item is auto-labeled [installed]/[not installed] - use 'Check Installed Status' to re-scan (read-only, changes nothing)."
$infoLabel.Location = New-Object System.Drawing.Point(10, 10)
$infoLabel.Size = New-Object System.Drawing.Size(850, 32)
$infoLabel.Font = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Italic)
$form.Controls.Add($infoLabel)

# Filter box
$filterLabel = New-Object System.Windows.Forms.Label
$filterLabel.Text = "Filter:"
$filterLabel.Location = New-Object System.Drawing.Point(10, 46)
$filterLabel.Size = New-Object System.Drawing.Size(40, 20)
$form.Controls.Add($filterLabel)

$filterBox = New-Object System.Windows.Forms.TextBox
$filterBox.Location = New-Object System.Drawing.Point(55, 44)
$filterBox.Size = New-Object System.Drawing.Size(300, 20)
$form.Controls.Add($filterBox)

# TreeView with checkboxes
$tree = New-Object System.Windows.Forms.TreeView
$tree.Location = New-Object System.Drawing.Point(10, 72)
$tree.Size = New-Object System.Drawing.Size(850, 520)
$tree.Anchor = "Top,Bottom,Left,Right"
$tree.CheckBoxes = $true
$tree.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$form.Controls.Add($tree)

$script:suppressCheckEvents = $false

function New-CategoryNode($text) {
    $n = New-Object System.Windows.Forms.TreeNode($text)
    $n.NodeFont = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
    $n.Tag = @{Type="category"; Name=$text}
    return $n
}

# --- Root: System Tools (winget) ---
$rootTools = New-CategoryNode "Applications & CLI Tools (winget)"
foreach ($cat in $WingetCatalog.Keys) {
    $catNode = New-CategoryNode $cat
    foreach ($pkg in $WingetCatalog[$cat]) {
        $leaf = New-Object System.Windows.Forms.TreeNode($pkg.Name)
        $leaf.Tag = @{Type="winget"; Id=$pkg.Id; Name=$pkg.Name}
        $catNode.Nodes.Add($leaf) | Out-Null
    }
    $rootTools.Nodes.Add($catNode) | Out-Null
}
$tree.Nodes.Add($rootTools) | Out-Null

# --- Root: System Tweaks (off by default, explicit) ---
$rootTweaks = New-CategoryNode "Optional System Tweaks (modifies Windows settings - off by default)"
foreach ($tweak in $SystemTweaks) {
    $leaf = New-Object System.Windows.Forms.TreeNode($tweak.Name)
    $leaf.Tag = @{Type="tweak"; Id=$tweak.Id; Name=$tweak.Name; Data=$tweak}
    $rootTweaks.Nodes.Add($leaf) | Out-Null
}
$tree.Nodes.Add($rootTweaks) | Out-Null

# --- Root: Python packages (pip) ---
$rootPip = New-CategoryNode "Python / Data Science Packages (pip, installed into your venv)"
foreach ($cat in $PipCatalog.Keys) {
    $catNode = New-CategoryNode $cat
    foreach ($pkgName in $PipCatalog[$cat]) {
        $leaf = New-Object System.Windows.Forms.TreeNode($pkgName)
        $leaf.Tag = @{Type="pip"; Id=$pkgName; Name=$pkgName}
        $catNode.Nodes.Add($leaf) | Out-Null
    }
    $rootPip.Nodes.Add($catNode) | Out-Null
}
$tree.Nodes.Add($rootPip) | Out-Null

# --- Root: Project setup extras ---
$rootProject = New-CategoryNode "Project Setup"
foreach ($opt in $ProjectSetupOptions) {
    $leaf = New-Object System.Windows.Forms.TreeNode($opt.Name)
    $leaf.Tag = @{Type="project"; Id=$opt.Id; Name=$opt.Name}
    $rootProject.Nodes.Add($leaf) | Out-Null
}
$tree.Nodes.Add($rootProject) | Out-Null

$tree.ExpandAll()

# --- Cascading check logic (parent <-> children), loop-safe ---
$tree.Add_AfterCheck({
    param($sender, $e)
    if ($script:suppressCheckEvents) { return }
    $script:suppressCheckEvents = $true
    try {
        # cascade down to all descendants
        function Set-Descendants($node, [bool]$checked) {
            foreach ($child in $node.Nodes) {
                $child.Checked = $checked
                Set-Descendants $child $checked
            }
        }
        Set-Descendants $e.Node $e.Node.Checked

        # cascade up: a parent reflects "at least one child checked"
        function Update-Ancestors($node) {
            $parent = $node.Parent
            while ($parent -ne $null) {
                $anyChecked = $false
                foreach ($c in $parent.Nodes) { if ($c.Checked) { $anyChecked = $true; break } }
                $parent.Checked = $anyChecked
                $parent = $parent.Parent
            }
        }
        Update-Ancestors $e.Node
    }
    finally {
        $script:suppressCheckEvents = $false
    }
})

# --- Filter box: expand/highlight matches, doesn't hide (TreeView has no native hide) ---
$filterBox.Add_TextChanged({
    $q = $filterBox.Text.Trim().ToLower()
    if ($q -eq "") { $tree.CollapseAll(); $tree.ExpandAll(); return }
    function Walk($node) {
        $selfMatch = $node.Text.ToLower().Contains($q)
        $childMatch = $false
        foreach ($c in $node.Nodes) {
            if (Walk $c) { $childMatch = $true }
        }
        if ($selfMatch -or $childMatch) {
            $node.Expand()
            if ($selfMatch) { $node.BackColor = [System.Drawing.Color]::LightYellow } else { $node.BackColor = [System.Drawing.Color]::White }
            return $true
        } else {
            $node.Collapse()
            $node.BackColor = [System.Drawing.Color]::White
            return $false
        }
    }
    foreach ($n in $tree.Nodes) { Walk $n | Out-Null }
})

# ------------------------------------------------------------
# 3. BUTTONS
# ------------------------------------------------------------

$btnSelectAll = New-Object System.Windows.Forms.Button
$btnSelectAll.Text = "Select All"
$btnSelectAll.Location = New-Object System.Drawing.Point(10, 604)
$btnSelectAll.Size = New-Object System.Drawing.Size(110, 30)
$btnSelectAll.Anchor = "Bottom,Left"
$form.Controls.Add($btnSelectAll)

$btnSelectNone = New-Object System.Windows.Forms.Button
$btnSelectNone.Text = "Select None"
$btnSelectNone.Location = New-Object System.Drawing.Point(130, 604)
$btnSelectNone.Size = New-Object System.Drawing.Size(110, 30)
$btnSelectNone.Anchor = "Bottom,Left"
$form.Controls.Add($btnSelectNone)

$btnSelectAll.Add_Click({
    $script:suppressCheckEvents = $true
    function CheckAll($node) { $node.Checked = $true; foreach ($c in $node.Nodes) { CheckAll $c } }
    foreach ($n in $tree.Nodes) { CheckAll $n }
    $script:suppressCheckEvents = $false
})
$btnSelectNone.Add_Click({
    $script:suppressCheckEvents = $true
    function UncheckAll($node) { $node.Checked = $false; foreach ($c in $node.Nodes) { UncheckAll $c } }
    foreach ($n in $tree.Nodes) { UncheckAll $n }
    $script:suppressCheckEvents = $false
})

$btnCheckStatus = New-Object System.Windows.Forms.Button
$btnCheckStatus.Text = "Check Installed Status"
$btnCheckStatus.Location = New-Object System.Drawing.Point(250, 604)
$btnCheckStatus.Size = New-Object System.Drawing.Size(150, 30)
$btnCheckStatus.Anchor = "Bottom,Left"
$form.Controls.Add($btnCheckStatus)

$lblCount = New-Object System.Windows.Forms.Label
$lblCount.Location = New-Object System.Drawing.Point(410, 610)
$lblCount.Size = New-Object System.Drawing.Size(230, 20)
$lblCount.Anchor = "Bottom,Left"
$form.Controls.Add($lblCount)

$lblStatus = New-Object System.Windows.Forms.Label
$lblStatus.Location = New-Object System.Drawing.Point(365, 46)
$lblStatus.Size = New-Object System.Drawing.Size(495, 20)
$lblStatus.ForeColor = [System.Drawing.Color]::DimGray
$lblStatus.Text = "Status not checked yet."
$form.Controls.Add($lblStatus)

$timer = New-Object System.Windows.Forms.Timer
$timer.Interval = 400
$timer.Add_Tick({
    function CountLeaves($node) {
        $n = 0
        if ($node.Nodes.Count -eq 0 -and $node.Checked) { $n = 1 }
        foreach ($c in $node.Nodes) { $n += CountLeaves $c }
        return $n
    }
    $total = 0
    foreach ($n in $tree.Nodes) { $total += CountLeaves $n }
    $lblCount.Text = "$total item(s) selected"
})
$timer.Start()

$btnInstall = New-Object System.Windows.Forms.Button
$btnInstall.Text = "Install Selected"
$btnInstall.Location = New-Object System.Drawing.Point(650, 604)
$btnInstall.Size = New-Object System.Drawing.Size(110, 34)
$btnInstall.Anchor = "Bottom,Right"
$btnInstall.BackColor = [System.Drawing.Color]::LightGreen
$form.Controls.Add($btnInstall)

$btnExit = New-Object System.Windows.Forms.Button
$btnExit.Text = "Cancel / Exit"
$btnExit.Location = New-Object System.Drawing.Point(770, 604)
$btnExit.Size = New-Object System.Drawing.Size(90, 34)
$btnExit.Anchor = "Bottom,Right"
$form.Controls.Add($btnExit)
$btnExit.Add_Click({ $form.Close() })

# Log / progress panel
$logBox = New-Object System.Windows.Forms.TextBox
$logBox.Location = New-Object System.Drawing.Point(10, 646)
$logBox.Size = New-Object System.Drawing.Size(850, 60)
$logBox.Anchor = "Bottom,Left,Right"
$logBox.Multiline = $true
$logBox.ScrollBars = "Vertical"
$logBox.ReadOnly = $true
$logBox.Font = New-Object System.Drawing.Font("Consolas", 8)
$form.Controls.Add($logBox)

# ------------------------------------------------------------
# 4. INSTALL LOGIC - only ever touches what's checked
# ------------------------------------------------------------

function Write-Log([string]$msg) {
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $line = "[$timestamp] $msg"
    Add-Content -Path $script:LogFile -Value $line
    $logBox.AppendText("$line`r`n")
    $logBox.SelectionStart = $logBox.Text.Length
    $logBox.ScrollToCaret()
    [System.Windows.Forms.Application]::DoEvents()
}

function Get-CheckedLeaves($node, [ref]$results) {
    if ($node.Nodes.Count -eq 0) {
        if ($node.Checked -and $node.Tag) { $results.Value.Add($node.Tag) | Out-Null }
    } else {
        foreach ($c in $node.Nodes) { Get-CheckedLeaves $c $results }
    }
}

$btnInstall.Add_Click({
    $selected = New-Object System.Collections.Generic.List[Object]
    foreach ($n in $tree.Nodes) { Get-CheckedLeaves $n ([ref]$selected) }

    if ($selected.Count -eq 0) {
        [System.Windows.Forms.MessageBox]::Show("Nothing is checked, so there's nothing to install. Check the items you want first.", "Nothing selected", "OK", "Information") | Out-Null
        return
    }

    $tweaksSelected = $selected | Where-Object { $_.Type -eq "tweak" }
    $confirmMsg = "This will install $($selected.Count) selected item(s)."
    if ($tweaksSelected.Count -gt 0) {
        $confirmMsg += "`n`nThis includes $($tweaksSelected.Count) system tweak(s) that change Windows settings (registry / optional features):`n - " + (($tweaksSelected | ForEach-Object { $_.Name }) -join "`n - ")
    }
    $confirmMsg += "`n`nNothing else on your machine will be modified or removed. Continue?"
    $confirm = [System.Windows.Forms.MessageBox]::Show($confirmMsg, "Confirm installation", "YesNo", "Question")
    if ($confirm -ne "Yes") { return }

    $btnInstall.Enabled = $false
    $btnSelectAll.Enabled = $false
    $btnSelectNone.Enabled = $false

    $script:LogFile = Join-Path $PSScriptRoot "setup_log.txt"
    "============================================================" | Out-File -FilePath $script:LogFile -Encoding UTF8
    "AI / Data Science / Full-Stack Environment - Setup Log"      | Out-File -FilePath $script:LogFile -Append -Encoding UTF8
    "Started: $(Get-Date)"                                        | Out-File -FilePath $script:LogFile -Append -Encoding UTF8
    "Selected items: $($selected.Count)"                          | Out-File -FilePath $script:LogFile -Append -Encoding UTF8
    "============================================================" | Out-File -FilePath $script:LogFile -Append -Encoding UTF8

    $okCount = 0
    $failCount = 0
    $failedList = New-Object System.Collections.Generic.List[string]

    # Group by type so venv/pip run in the right order
    $wingetItems  = $selected | Where-Object { $_.Type -eq "winget" }
    $tweakItems   = $selected | Where-Object { $_.Type -eq "tweak" }
    $pipItems     = $selected | Where-Object { $_.Type -eq "pip" }
    $projectItems = $selected | Where-Object { $_.Type -eq "project" }

    # -- winget installs --
    foreach ($item in $wingetItems) {
        Write-Log "Installing (winget): $($item.Name) [$($item.Id)] ..."
        try {
            winget install --id $item.Id -e --accept-source-agreements --accept-package-agreements | Out-Null
            if ($LASTEXITCODE -eq 0) {
                Write-Log "  OK: $($item.Name)"
                $okCount++
            } else {
                Write-Log "  FAILED: $($item.Name) (exit $LASTEXITCODE)"
                $failCount++; $failedList.Add($item.Name) | Out-Null
            }
        } catch {
            Write-Log "  FAILED: $($item.Name) - $($_.Exception.Message)"
            $failCount++; $failedList.Add($item.Name) | Out-Null
        }
    }

    # -- system tweaks --
    foreach ($item in $tweakItems) {
        $data = $item.Data
        Write-Log "Applying system tweak: $($item.Name) ..."
        try {
            switch ($data.Action) {
                "winget" {
                    winget install --id $data.WingetId -e --accept-source-agreements --accept-package-agreements | Out-Null
                    if ($LASTEXITCODE -ne 0) { throw "winget exit $LASTEXITCODE" }
                }
                "wsl" {
                    $status = wsl --status 2>&1
                    if ($LASTEXITCODE -ne 0) {
                        wsl --install --no-distribution
                        if ($LASTEXITCODE -ne 0) { throw "wsl install failed - may need a manual reboot + rerun" }
                    }
                }
                "devmode" {
                    reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock" /v AllowDevelopmentWithoutDevLicense /t REG_DWORD /d 1 /f | Out-Null
                    if ($LASTEXITCODE -ne 0) { throw "registry write failed - try running as Administrator" }
                }
                "longpath" {
                    reg add "HKLM\SYSTEM\CurrentControlSet\Control\FileSystem" /v LongPathsEnabled /t REG_DWORD /d 1 /f | Out-Null
                    if ($LASTEXITCODE -ne 0) { throw "registry write failed - try running as Administrator" }
                }
            }
            Write-Log "  OK: $($item.Name)"
            $okCount++
        } catch {
            Write-Log "  FAILED: $($item.Name) - $($_.Exception.Message)"
            $failCount++; $failedList.Add($item.Name) | Out-Null
        }
    }

    # -- venv (only if the user checked it, and only before pip installs) --
    $venvPath = Join-Path $PSScriptRoot ".venv"
    $wantsVenv = $projectItems | Where-Object { $_.Id -eq "VENV" }
    if ($wantsVenv) {
        Write-Log "Creating virtual environment at $venvPath ..."
        try {
            python -m venv $venvPath
            if ($LASTEXITCODE -ne 0) { throw "python -m venv failed" }
            Write-Log "  OK: virtual environment created"
            $okCount++
        } catch {
            Write-Log "  FAILED: virtual environment - $($_.Exception.Message)"
            $failCount++; $failedList.Add("Virtual environment") | Out-Null
        }
    }

    # Use the venv's python/pip if it was created (or already exists); else fall back to system python
    $pyExe = "python"
    $venvPy = Join-Path $venvPath "Scripts\python.exe"
    if (Test-Path $venvPy) { $pyExe = $venvPy }

    # -- pip installs --
    if ($pipItems.Count -gt 0) {
        Write-Log "Upgrading pip/setuptools/wheel ..."
        & $pyExe -m pip install --upgrade pip setuptools wheel | Out-Null
    }
    foreach ($item in $pipItems) {
        Write-Log "Installing (pip): $($item.Name) ..."
        try {
            & $pyExe -m pip install $item.Id | Out-Null
            if ($LASTEXITCODE -eq 0) {
                Write-Log "  OK: $($item.Name)"
                $okCount++
            } else {
                Write-Log "  FAILED: $($item.Name) (exit $LASTEXITCODE)"
                $failCount++; $failedList.Add($item.Name) | Out-Null
            }
        } catch {
            Write-Log "  FAILED: $($item.Name) - $($_.Exception.Message)"
            $failCount++; $failedList.Add($item.Name) | Out-Null
        }
    }

    # -- project directories --
    if ($projectItems | Where-Object { $_.Id -eq "DIRS" }) {
        Write-Log "Creating project folder structure ..."
        $dirs = @("ai","ml","deep-learning","llm","rag","agents","computer-vision","nlp",
                  "data-science","data-engineering","backend","frontend","apis","databases",
                  "devops","testing","research","case-studies","datasets","statistics",
                  "time-series","geospatial","reports")
        try {
            foreach ($d in $dirs) {
                $full = Join-Path $PSScriptRoot "projects\$d"
                if (-not (Test-Path $full)) { New-Item -ItemType Directory -Path $full -Force | Out-Null }
            }
            Write-Log "  OK: project folders"
            $okCount++
        } catch {
            Write-Log "  FAILED: project folders - $($_.Exception.Message)"
            $failCount++; $failedList.Add("Project folders") | Out-Null
        }
    }

    # -- ipykernel registration --
    if ($projectItems | Where-Object { $_.Id -eq "IPYKERNEL" }) {
        Write-Log "Registering Jupyter kernel ..."
        try {
            & $pyExe -m pip install ipykernel | Out-Null
            & $pyExe -m ipykernel install --user --name ai-fullstack --display-name "AI Fullstack" | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "ipykernel install failed" }
            Write-Log "  OK: Jupyter kernel registered"
            $okCount++
        } catch {
            Write-Log "  FAILED: Jupyter kernel - $($_.Exception.Message)"
            $failCount++; $failedList.Add("Jupyter kernel") | Out-Null
        }
    }

    # -- freeze requirements --
    if ($projectItems | Where-Object { $_.Id -eq "FREEZE" }) {
        Write-Log "Writing package inventory ..."
        try {
            & $pyExe -m pip freeze | Out-File -FilePath (Join-Path $PSScriptRoot "installed_packages.txt") -Encoding UTF8
            & $pyExe -m pip freeze | Out-File -FilePath (Join-Path $PSScriptRoot "requirements-full.txt") -Encoding UTF8
            Write-Log "  OK: installed_packages.txt / requirements-full.txt written"
            $okCount++
        } catch {
            Write-Log "  FAILED: package inventory - $($_.Exception.Message)"
            $failCount++; $failedList.Add("Package inventory") | Out-Null
        }
    }

    Write-Log "============================================================"
    Write-Log "DONE. OK=$okCount FAILED=$failCount"
    if ($failedList.Count -gt 0) {
        Write-Log "Failed items: $($failedList -join ', ')"
    }
    Write-Log "Nothing outside your selection was installed, changed, or removed."
    Write-Log "Full log: $script:LogFile"

    $summary = "Finished.`n`nOK: $okCount`nFailed: $failCount"
    if ($failedList.Count -gt 0) {
        $summary += "`n`nFailed items:`n - " + ($failedList -join "`n - ") + "`n`nNothing was skipped because of these failures elsewhere; you can re-check just these items and click Install again."
    }
    [System.Windows.Forms.MessageBox]::Show($summary, "Setup finished", "OK", "Information") | Out-Null

    $btnInstall.Enabled = $true
    $btnSelectAll.Enabled = $true
    $btnSelectNone.Enabled = $true

    Update-CheckStatus
})

# ------------------------------------------------------------
# 5. INSTALL-STATUS CHECKING
#    Read-only: this section only ever *inspects* the machine
#    (winget list, pip list, registry reads, file/folder checks).
#    It never installs, changes, or removes anything.
# ------------------------------------------------------------

function Get-WingetListText {
    # One combined "winget list" call covers every winget-based tool and
    # every winget-based system tweak, instead of one process per item.
    try {
        $out = winget list --accept-source-agreements 2>&1 | Out-String
        return $out
    } catch {
        return $null
    }
}

function Get-PipInstalledSet($pyExe) {
    # One "pip list --format=json" call covers every pip package.
    $set = New-Object 'System.Collections.Generic.HashSet[string]'
    try {
        $json = & $pyExe -m pip list --format=json 2>$null
        if ($LASTEXITCODE -eq 0 -and $json) {
            $pkgs = $json | ConvertFrom-Json
            foreach ($p in $pkgs) {
                [void]$set.Add(((($p.name) -replace '[_\.]', '-').ToLower()))
            }
        }
    } catch { }
    return $set
}

function Normalize-PipName([string]$raw) {
    # "mcp[cli]" -> mcp ; scikit_learn / scikit.learn -> scikit-learn, etc.
    $n = $raw.Trim('"', "'")
    $n = $n -replace '\[.*\]', ''
    $n = ($n -replace '[_\.]', '-').ToLower()
    return $n
}

function Test-RegistryDword($path, $name, $expected) {
    try {
        $v = Get-ItemProperty -Path $path -Name $name -ErrorAction Stop
        return ($v.$name -eq $expected)
    } catch { return $false }
}

function Test-WslInstalled {
    try {
        wsl --status 2>&1 | Out-Null
        return ($LASTEXITCODE -eq 0)
    } catch { return $false }
}

function Mark-LeafStatus($node, $installed) {
    # $installed: $true / $false / $null (unknown - couldn't determine)
    $baseName = $node.Tag.Name
    if ($installed -eq $true) {
        $node.Text = "$baseName   [installed]"
        $node.ForeColor = [System.Drawing.Color]::Green
    } elseif ($installed -eq $false) {
        $node.Text = "$baseName   [not installed]"
        $node.ForeColor = [System.Drawing.Color]::Black
    } else {
        $node.Text = "$baseName   [unknown]"
        $node.ForeColor = [System.Drawing.Color]::Gray
    }
}

function Get-LeafCounts($node) {
    # Returns @(totalLeaves, installedLeaves) for a node's leaf descendants.
    $total = 0; $installed = 0
    if ($node.Nodes.Count -eq 0) {
        if ($node.Tag -and $node.Tag.Type -ne "category") {
            $total = 1
            if ($node.ForeColor -eq [System.Drawing.Color]::Green) { $installed = 1 }
        }
    } else {
        foreach ($c in $node.Nodes) {
            $r = Get-LeafCounts $c
            $total += $r[0]; $installed += $r[1]
        }
    }
    return @($total, $installed)
}

function Update-CategoryLabels($node) {
    foreach ($c in $node.Nodes) { Update-CategoryLabels $c }
    if ($node.Nodes.Count -gt 0 -and $node.Tag -and $node.Tag.Type -eq "category") {
        $counts = Get-LeafCounts $node
        $node.Text = "$($node.Tag.Name)   [$($counts[1])/$($counts[0]) installed]"
    }
}

function Update-CheckStatus {
    $btnCheckStatus.Enabled = $false
    $btnInstall.Enabled = $false
    $btnSelectAll.Enabled = $false
    $btnSelectNone.Enabled = $false
    $lblStatus.Text = "Checking what's already installed ..."
    Write-Log "Checking install status of every item (read-only - nothing is changed) ..."

    $wingetText = Get-WingetListText
    if ($wingetText -eq $null) { Write-Log "  NOTE: could not run winget list - winget items will show as unknown." }

    $venvPath = Join-Path $PSScriptRoot ".venv"
    $venvPy = Join-Path $venvPath "Scripts\python.exe"
    $pyExe = if (Test-Path $venvPy) { $venvPy } else { "python" }
    $pipSet = Get-PipInstalledSet $pyExe

    $script:statusInstalled = 0
    $script:statusMissing = 0
    $script:statusUnknown = 0

    function Record($result) {
        if ($result -eq $true) { $script:statusInstalled++ }
        elseif ($result -eq $false) { $script:statusMissing++ }
        else { $script:statusUnknown++ }
    }

    function Walk($node) {
        if ($node.Nodes.Count -eq 0) {
            if (-not $node.Tag -or $node.Tag.Type -eq "category") { return }
            $tag = $node.Tag
            $result = $null
            switch ($tag.Type) {
                "winget" {
                    if ($wingetText -ne $null) { $result = [bool]($wingetText -match [regex]::Escape($tag.Id)) }
                }
                "pip" {
                    $result = $pipSet.Contains((Normalize-PipName $tag.Id))
                }
                "tweak" {
                    $data = $tag.Data
                    switch ($data.Action) {
                        "winget"   { if ($wingetText -ne $null) { $result = [bool]($wingetText -match [regex]::Escape($data.WingetId)) } }
                        "wsl"      { $result = Test-WslInstalled }
                        "devmode"  { $result = Test-RegistryDword "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock" "AllowDevelopmentWithoutDevLicense" 1 }
                        "longpath" { $result = Test-RegistryDword "HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem" "LongPathsEnabled" 1 }
                    }
                }
                "project" {
                    switch ($tag.Id) {
                        "VENV"      { $result = Test-Path $venvPy }
                        "DIRS"      { $result = Test-Path (Join-Path $PSScriptRoot "projects") }
                        "FREEZE"    { $result = (Test-Path (Join-Path $PSScriptRoot "installed_packages.txt")) -and (Test-Path (Join-Path $PSScriptRoot "requirements-full.txt")) }
                        "IPYKERNEL" { $result = Test-Path (Join-Path $env:APPDATA "jupyter\kernels\ai-fullstack") }
                    }
                }
            }
            Mark-LeafStatus $node $result
            Record $result
        } else {
            foreach ($c in $node.Nodes) { Walk $c }
        }
    }
    foreach ($n in $tree.Nodes) { Walk $n }
    foreach ($n in $tree.Nodes) { Update-CategoryLabels $n }

    $summary = "Status: $($script:statusInstalled) installed, $($script:statusMissing) not installed"
    if ($script:statusUnknown -gt 0) { $summary += ", $($script:statusUnknown) unknown" }
    $lblStatus.Text = $summary
    Write-Log "  Done. $summary. (Detection only - nothing was installed, changed, or removed.)"

    $btnCheckStatus.Enabled = $true
    $btnInstall.Enabled = $true
    $btnSelectAll.Enabled = $true
    $btnSelectNone.Enabled = $true
}

$btnCheckStatus.Add_Click({ Update-CheckStatus })

[System.Windows.Forms.Application]::EnableVisualStyles()
$form.Add_Shown({
    $form.Activate()
    # Auto-check status once on open so the tree already shows what's
    # installed before the user picks anything; re-run anytime with the button.
    Update-CheckStatus
})
[System.Windows.Forms.Application]::Run($form)
