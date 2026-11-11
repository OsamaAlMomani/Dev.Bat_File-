namespace AiSetupUi;

/// <summary>Everything that shows up in the tree. Edit this file to add or remove items.</summary>
internal static class Catalog
{
    public static readonly (string Category, WingetPackage[] Items)[] Winget =
    {
        ("System Development Tools", new[]
        {
            new WingetPackage("Git.Git", "Git"),
            new WingetPackage("GitHub.cli", "GitHub CLI"),
            new WingetPackage("Microsoft.VisualStudioCode", "Visual Studio Code"),
            new WingetPackage("Microsoft.WindowsTerminal", "Windows Terminal"),
            new WingetPackage("Docker.DockerDesktop", "Docker Desktop"),
            new WingetPackage("OpenJS.NodeJS.LTS", "Node.js LTS"),
            new WingetPackage("Microsoft.PowerShell", "PowerShell 7"),
            new WingetPackage("Postman.Postman", "Postman"),
            new WingetPackage("JetBrains.Toolbox", "JetBrains Toolbox"),
        }),
        ("Research / Statistical Computing", new[]
        {
            new WingetPackage("RProject.R", "R"),
            new WingetPackage("Posit.RStudio", "RStudio"),
            new WingetPackage("MiKTeX.MiKTeX", "MiKTeX (LaTeX)"),
            new WingetPackage("JGraph.Draw", "draw.io"),
            new WingetPackage("Zotero.Zotero", "Zotero"),
            new WingetPackage("Pandoc.Pandoc", "Pandoc"),
            new WingetPackage("Anaconda.Miniconda3", "Miniconda3"),
        }),
        ("Cloud / DevOps CLIs", new[]
        {
            new WingetPackage("Kubernetes.kubectl", "kubectl"),
            new WingetPackage("Helm.Helm", "Helm"),
            new WingetPackage("Amazon.AWSCLI", "AWS CLI"),
            new WingetPackage("Microsoft.AzureCLI", "Azure CLI"),
            new WingetPackage("Google.CloudSDK", "Google Cloud SDK"),
            new WingetPackage("Hashicorp.Terraform", "Terraform"),
            new WingetPackage("Hashicorp.Vault", "Vault"),
            new WingetPackage("jqlang.jq", "jq"),
            new WingetPackage("curl.curl", "curl"),
        }),
        ("Database Software", new[]
        {
            new WingetPackage("PostgreSQL.PostgreSQL", "PostgreSQL"),
            new WingetPackage("MongoDB.Server", "MongoDB"),
            new WingetPackage("Redis.Redis", "Redis"),
        }),
    };

    public static readonly Tweak[] Tweaks =
    {
        new("VCREDIST64", "Microsoft VC++ Redistributable (x64)", TweakAction.Winget, "Microsoft.VCRedist.2015+.x64"),
        new("VCREDIST86", "Microsoft VC++ Redistributable (x86)", TweakAction.Winget, "Microsoft.VCRedist.2015+.x86"),
        new("DOTNETRT", ".NET 8 Desktop Runtime", TweakAction.Winget, "Microsoft.DotNet.DesktopRuntime.8"),
        new("DOTNETSDK", ".NET 8 SDK", TweakAction.Winget, "Microsoft.DotNet.SDK.8"),
        new("WSL2", "WSL2 (Windows Subsystem for Linux)", TweakAction.Wsl),
        new("DEVMODE", "Enable Windows Developer Mode (registry)", TweakAction.DevMode),
        new("LONGPATH", "Enable NTFS long path support (registry)", TweakAction.LongPath),
    };

    public static readonly (string Category, string[] Packages)[] Pip =
    {
        ("Scientific Computing", new[] { "numpy", "scipy", "pandas", "polars", "sympy", "statsmodels", "numba", "cython", "bottleneck", "numexpr" }),
        ("Machine Learning", new[] { "scikit-learn", "xgboost", "lightgbm", "catboost", "imbalanced-learn", "mlxtend", "feature-engine", "shap", "lime", "yellowbrick", "optuna", "hyperopt" }),
        ("Deep Learning", new[] { "torch", "torchvision", "torchaudio", "tensorflow", "keras", "jax", "flax", "onnx", "onnxruntime", "onnxruntime-gpu", "tensorboard" }),
        ("Computer Vision", new[] { "opencv-python", "opencv-contrib-python", "scikit-image", "imageio", "imageio-ffmpeg", "pillow", "albumentations", "ultralytics", "timm", "torchmetrics" }),
        ("NLP", new[] { "nltk", "spacy", "gensim", "textblob", "textacy", "stanza", "flair", "vaderSentiment", "sentencepiece", "sacremoses", "conllu" }),
        ("Transformers / LLM", new[] { "transformers", "tokenizers", "datasets", "huggingface-hub", "safetensors", "accelerate", "peft", "trl", "bitsandbytes", "sentence-transformers", "evaluate" }),
        ("Generative AI APIs", new[] { "openai", "anthropic", "cohere", "google-genai", "groq", "mistralai", "ollama" }),
        ("RAG / Vector Databases", new[] { "langchain", "langchain-community", "langchain-openai", "langchain-anthropic", "langgraph", "llama-index", "chromadb", "faiss-cpu", "qdrant-client", "weaviate-client", "pinecone", "pymilvus" }),
        ("AI Agents / MCP", new[] { "mcp[cli]", "crewai", "autogen-agentchat", "semantic-kernel", "smolagents" }),
        ("Chinese AI Frameworks / Distributed Training", new[] { "paddlepaddle", "paddlenlp", "paddleocr", "paddlehub", "modelscope", "ms-swift", "colossalai", "bmtrain", "FlagEmbedding", "flagai", "auto-gptq", "autoawq", "optimum", "vllm" }),
        ("Chinese LLM Provider SDKs", new[] { "dashscope", "zhipuai", "qianfan", "volcengine-python-sdk", "zai-sdk" }),
        ("API / Backend", new[] { "fastapi", "uvicorn", "gunicorn", "flask", "django", "djangorestframework", "starlette", "pydantic", "pydantic-settings", "httpx", "requests", "aiohttp", "websockets", "python-multipart", "python-dotenv", "email-validator", "orjson", "msgspec", "graphene", "strawberry-graphql", "gql", "grpcio", "grpcio-tools", "protobuf" }),
        ("Database / ORM", new[] { "sqlalchemy", "alembic", "psycopg", "psycopg2-binary", "asyncpg", "pymysql", "mysqlclient", "pymongo", "motor", "redis", "elasticsearch", "neo4j", "influxdb-client" }),
        ("Data Engineering", new[] { "pyarrow", "fastparquet", "duckdb", "dask", "pyspark", "great-expectations", "dbt-core", "dbt-postgres", "apache-airflow" }),
        ("Visualization", new[] { "matplotlib", "seaborn", "plotly", "bokeh", "altair", "folium", "dash", "streamlit", "gradio", "panel", "holoviews", "hvplot" }),
        ("Web Scraping / Browser Automation", new[] { "beautifulsoup4", "lxml", "html5lib", "requests-html", "selenium", "scrapy", "playwright", "trafilatura", "newspaper4k" }),
        ("Testing / QA", new[] { "pytest", "pytest-cov", "pytest-asyncio", "pytest-mock", "pytest-xdist", "hypothesis", "mock", "responses", "respx", "locust", "allure-pytest" }),
        ("Code Quality", new[] { "black", "isort", "ruff", "flake8", "mypy", "pylint", "pre-commit", "ipython", "ipdb", "rich", "typer", "click", "loguru" }),
        ("Jupyter / Experiment Tracking", new[] { "jupyter", "jupyterlab", "notebook", "ipykernel", "nbconvert", "nbformat", "mlflow", "wandb", "dvc", "clearml", "dagshub", "evidently" }),
        ("Model Serving / Deployment", new[] { "bentoml", "ray", "vllm", "litserve", "celery", "dramatiq" }),
        ("Audio / Speech", new[] { "librosa", "soundfile", "pydub", "SpeechRecognition", "openai-whisper", "faster-whisper", "webrtcvad" }),
        ("Networking / Security / Auth", new[] { "cryptography", "PyJWT", "passlib", "bcrypt", "authlib", "oauthlib", "requests-oauthlib", "paramiko", "dnspython" }),
        ("Dataset Acquisition / Research Data", new[] { "kaggle", "opendatasets", "ucimlrepo", "openml", "pydataset", "datacommons", "sodapy", "pytrends", "wbdata", "yfinance", "quandl", "fredapi" }),
        ("Advanced / Research Statistics", new[] { "pingouin", "scikit-posthocs", "researchpy", "statannotations", "linearmodels", "patsy", "formulaic", "dowhy", "econml", "causalml", "causal-learn", "lifelines", "scikit-survival" }),
        ("Bayesian Statistics / Probabilistic Modeling", new[] { "pymc", "arviz", "numpyro", "pystan", "bambi", "pomegranate" }),
        ("Advanced Time Series / Forecasting", new[] { "prophet", "pmdarima", "sktime", "tsfresh", "darts", "tbats", "neuralprophet" }),
        ("Geospatial / Network / Graph Analysis", new[] { "geopandas", "shapely", "pyproj", "rasterio", "geopy", "osmnx", "networkx", "python-igraph", "pygraphviz" }),
        ("Experimental Design / Survey / A-B Testing", new[] { "pyDOE2", "SALib", "statsmodels", "pyreadstat", "semopy", "factor-analyzer" }),
        ("Reproducible Research / Workflow / Reporting", new[] { "papermill", "snakemake", "scholarly", "arxiv", "pyzotero", "bibtexparser", "python-docx", "openpyxl", "xlsxwriter", "weasyprint", "markdown", "quarto-cli" }),
    };

    public static readonly ProjectOption[] ProjectOptions =
    {
        new("VENV", "Create a Python virtual environment (.venv)"),
        new("DIRS", "Create the projects\\ folder structure (ai, ml, llm, rag, backend, frontend, ...)"),
        new("FREEZE", "Write installed_packages.txt / requirements-full.txt after install"),
        new("IPYKERNEL", "Register a Jupyter kernel for this environment"),
    };

    public static readonly string[] ProjectDirs =
    {
        "ai", "ml", "deep-learning", "llm", "rag", "agents", "computer-vision", "nlp",
        "data-science", "data-engineering", "backend", "frontend", "apis", "databases",
        "devops", "testing", "research", "case-studies", "datasets", "statistics",
        "time-series", "geospatial", "reports",
    };

    /// <summary>Normalized names of every catalog pip package, used by the extras scan.</summary>
    public static readonly HashSet<string> PipNames = BuildPipNames();

    private static HashSet<string> BuildPipNames()
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (_, packages) in Pip)
            foreach (var pkg in packages)
                set.Add(PipName.Normalize(pkg));
        return set;
    }
}
