# See here for image contents: https://github.com/microsoft/vscode-dev-containers/tree/v0.203.0/containers/ubuntu/.devcontainer/base.Dockerfile

# [Choice] Ubuntu version (use hirsuite or bionic on local arm64/Apple Silicon): hirsute, focal, bionic
ARG VARIANT="hirsute"
#FROM mcr.microsoft.com/vscode/devcontainers/base:0-${VARIANT}
FROM mcr.microsoft.com/vscode/devcontainers/dotnet:7.0
# [Optional] Uncomment this section to install additional OS packages.
# RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
#     && apt-get -y install --no-install-recommends <your-package-list-here>

# Install hey
RUN apt-get update && \
    apt-get install wget -y && \
    wget https://hey-release.s3.us-east-2.amazonaws.com/hey_linux_amd64 && \
    chmod +x hey_linux_amd64 && \
    mv hey_linux_amd64 /usr/local/bin/hey 
