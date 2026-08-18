# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### 🚀 New features
- Initialize project

### 🐛 Bug fixes
- Configure Serilog from `appsettings.json` and registered services so API sinks remain effective in containers.
- Keep the Scalar API reference reachable in every environment and redirect version-prefixed asset requests to the canonical path.
- Map the default health check endpoints on the API and expose them in every environment so orchestrators can observe readiness.
- Start the pgAdmin management container from its own pinned image instead of the PostgreSQL one, and remove the pgWeb container that had no pinned image declared.