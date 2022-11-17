FROM debian:bullseye-slim

LABEL org.opencontainers.image.authors="tpilius@gmail.com;admin@minenet.at"
LABEL org.opencontainers.image.source="https://github.com/tpill90/battlenet-lancache-prefill"

RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    libncursesw5 \
    locales && \
  rm -rf /var/lib/apt/lists/*

RUN sed -i '/en_US.UTF-8/s/^# //' /etc/locale.gen && \
  dpkg-reconfigure --frontend=noninteractive locales && \
  update-locale LANG=en_US.UTF-8

ENV LANG=en_US.UTF-8
ENV LANGUAGE=en_US:en
ENV LC_ALL=en_US.UTF-8
ENV TERM=xterm-256color

COPY  /publish/BatteNetPrefill /
RUN chmod +x /BatteNetPrefill

ENTRYPOINT [ "/BatteNetPrefill" ]
