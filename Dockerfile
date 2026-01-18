FROM python:3.11-slim

RUN apt-get update && apt-get install -y --no-install-recommends libpq5 libpq-dev

RUN pip install --no-cache-dir requests
RUN pip install --no-cache-dir 'psycopg[binary]'

WORKDIR /work
