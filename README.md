# Dev.Bat_File-
# AI / Data Science / Full-Stack Environment Setup UI

A **flexible, friendly, and safe** Windows installer for AI, Machine Learning, Deep Learning, Data Science, and full‑stack development tools.

![UI Screenshot](screenshot.png) *(you can add a screenshot later)*

---

## 🔍 What This Tool Does

- **Gives you a checkbox tree** of hundreds of popular tools, Python libraries, and system tweaks.
- **You pick exactly what you want** – nothing is pre‑selected, and nothing gets installed without your explicit consent.
- **Only the checked items are installed** – your existing software is never touched, upgraded, or removed.
- **Read‑only status check** lets you see what’s already installed on your machine before you choose.
- **All install actions are logged** to `setup_log.txt` for complete transparency.

---

## ✨ Features

- **400+ Python packages** (NumPy, PyTorch, Transformers, LangChain, FastAPI, …) grouped by category.
- **60+ Windows applications** via **winget** (Git, VS Code, Docker, Node.js, PostgreSQL, …).
- **Optional system tweaks** (WSL2, Developer Mode, long path support, VC++ Redistributables).
- **Project setup** – automatically creates a Python virtual environment, a structured `projects/` folder, registers a Jupyter kernel, and saves a `requirements-full.txt`.
- **Smart filtering** – quickly find items by name.
- **Cascading checkboxes** – check a category to select all its children; unchecking a child updates the parent state.
- **Non‑destructive** – everything is additive; nothing is removed or overwritten.

---

## 📦 Prerequisites

- **Windows 10 / 11** (the script is PowerShell‑based).
- **PowerShell** (built‑in).
- **Internet connection** for downloading packages.
- **Administrator privileges** are **recommended** for system tweaks (registry changes, WSL2 installation).  
  *If you run without admin, most winget and pip installs will still work, but system tweaks may fail.*

---

## 🚀 How to Use

### 1. Download the files

Place both files in the same folder:

### 2. Launch the UI

Double‑click `Launch_Setup_UI.bat`.  
*(Alternatively, right‑click and “Run as Administrator” if you plan to use system tweaks.)*

A PowerShell window will appear briefly, then the **Setup UI** opens.

### 3. Explore and select items

- **Expand any category** to see the tools/libraries inside.
- **Check the box** next to anything you want to install.
- **Use the filter box** to highlight items that match your search.
- **Parent checkboxes** reflect the state of their children – check a whole category with one click.

### 4. Check what’s already installed (optional)

Click **“Check Installed Status”** – the UI will scan your system and label each item as:

- `[installed]` – already present on your machine.
- `[not installed]` – will be installed if you check it.
- `[unknown]` – could not be determined (e.g., winget not found).

This step is **read‑only** – it changes nothing on your system.

### 5. Install your selection

1. Click **“Install Selected”**.
2. A confirmation dialog shows the number of items and warns if system tweaks are included.
3. Click **Yes** to start the installation.

The log panel at the bottom of the window shows live progress.  
When done, a summary popup lists successful and failed items.

### 6. (Optional) Review the log

A full log is written to `setup_log.txt` in the same folder.  
It contains timestamps, commands executed, and any errors – useful for troubleshooting.

---

## 📂 What Gets Installed (Examples)

### Windows Applications (via winget)

- **System Development**: Git, GitHub CLI, VS Code, Windows Terminal, Docker Desktop, Node.js, PowerShell 7, Postman, JetBrains Toolbox.
- **Research / Statistics**: R, RStudio, MiKTeX, draw.io, Zotero, Pandoc, Miniconda3.
- **Cloud / DevOps**: kubectl, Helm, AWS CLI, Azure CLI, Google Cloud SDK, Terraform, Vault, jq, curl.
- **Databases**: PostgreSQL, MongoDB, Redis.

### Python Packages (via pip)

- **Scientific Computing**: numpy, scipy, pandas, polars, sympy, statsmodels, numba…
- **Machine Learning**: scikit‑learn, xgboost, lightgbm, catboost, shap, optuna…
- **Deep Learning**: torch, tensorflow, keras, jax, onnx, tensorboard…
- **Computer Vision**: opencv‑python, scikit‑image, pillow, albumentations, ultralytics, timm…
- **NLP / LLMs**: transformers, tokenizers, datasets, huggingface‑hub, accelerate, peft, vllm, sentence‑transformers…
- **Generative AI / RAG**: openai, anthropic, cohere, langchain, llama‑index, chromadb, faiss‑cpu, qdrant…
- **Chinese AI Ecosystems**: paddlepaddle, modelscope, dashscope, zhipuai, qianfan, colossalai, FlagEmbedding…
- **API / Backend**: fastapi, uvicorn, flask, django, pydantic, httpx, aiohttp, grpcio…
- **Databases / ORM**: sqlalchemy, alembic, psycopg, asyncpg, pymongo, redis, elasticsearch…
- **Data Engineering**: pyarrow, duckdb, dask, pyspark, great‑expectations, dbt, apache‑airflow.
- **Visualization**: matplotlib, seaborn, plotly, bokeh, altair, streamlit, gradio, dash.
- **Testing / QA**: pytest, hypothesis, locust, allure‑pytest.
- …and many more.

*(The full list is inside the script – you can easily add or remove packages.)*

### System Tweaks (opt‑in, off by default)

- **VC++ Redistributables** (x64 and x86)
- **.NET 8 Desktop Runtime and SDK**
- **WSL2** (Windows Subsystem for Linux)
- **Windows Developer Mode** (registry)
- **NTFS long path support** (registry)

### Project Setup (optional)

- **Create a Python virtual environment** (`.venv` in the script folder).
- **Create a `projects/` folder structure** with subfolders for AI, ML, LLM, RAG, backend, frontend, etc.
- **Write `installed_packages.txt` / `requirements-full.txt`** – a snapshot of all pip packages.
- **Register a Jupyter kernel** named *“AI Fullstack”* for use in Jupyter notebooks.

---

## ⚙️ Customizing the Catalog

The script is **data‑driven** – you can easily add or remove items by editing the catalog arrays inside `AI_Setup_UI.ps1`:

- `$WingetCatalog` – for winget applications.
- `$SystemTweaks` – for system‑level tweaks.
- `$PipCatalog` – for Python packages.
- `$ProjectSetupOptions` – for extra steps.

Look for the `# 1. DATA` section near the top of the script.

---

## 🛠 Troubleshooting

| Issue | Possible solution |
|-------|-------------------|
| **“winget not found”** | Install [App Installer](https://apps.microsoft.com/detail/9nblggh4nns1) from the Microsoft Store. |
| **Permission errors** | Run the .bat file **as Administrator** (right‑click → Run as administrator). |
| **Python not found** | Install Python from python.org or via winget (`Microsoft.Python`). The script will fall back to system python if `.venv` doesn’t exist. |
| **Pip install fails** | Check your internet connection; some packages are large and may time out. Retry later or install individually. |
| **Status check shows “unknown”** | Ensure winget is available and that you have a working Python/pip installation. |
| **The UI doesn’t open** | Make sure PowerShell execution policy allows scripts (the .bat file already bypasses it). Try running `powershell -ExecutionPolicy Bypass -File AI_Setup_UI.ps1` manually. |

---

## 📝 Logging

All actions are written to `setup_log.txt` in the script folder.  
The log includes:

- Start time and selection count.
- Each install command (winget, pip, registry tweaks, etc.) with success/failure.
- Final summary with counts and a list of failed items (if any).

You can review this file after the run to debug any issues.

---

## 🧪 Safety & Philosophy

> **Nothing in this script installs, changes, or removes anything on its own.**  
> You get a checkbox tree; you pick exactly what you want; only the checked items are installed.  
> Nothing pre‑installed on your machine is ever touched, upgraded, or deleted – it only **adds** what you explicitly check.

This tool is designed to be **safe for production machines** – you can run it without fear of breaking your existing environment.

---

## 📄 License

This project is open‑source. Feel free to use, modify, and share.  
*(If you want a specific license, add a `LICENSE` file – MIT is a popular choice.)*

---

## 🤝 Contributing

Found a bug? Want to add a new package?  
Pull requests and issue reports are welcome!

---

## 📧 Contact

For questions or suggestions, please open an issue on GitHub.
Osamamomani.idea@gmail.com

---

**Happy coding!** 🚀
