CREATE TABLE users
(
    id       SERIAL                  NOT NULL PRIMARY KEY,
    token    VARCHAR(1024)           NOT NULL,
    disabled BOOLEAN   DEFAULT FALSE NOT NULL,
    created  TIMESTAMP DEFAULT NULL
);
ALTER TABLE users
    ALTER COLUMN created SET DEFAULT NOW();


CREATE TABLE dnys
(
    id            SERIAL        NOT NULL PRIMARY KEY,
    time          TIME          NOT NULL,
    min_latitude  DECIMAL(9, 6) NOT NULL,
    min_longitude DECIMAL(9, 6) NOT NULL,
    max_latitude  DECIMAL(9, 6) NOT NULL,
    max_longitude DECIMAL(9, 6) NOT NULL,
    trains_count  SMALLINT      NOT NULL,
    timestamp     TIMESTAMP     NOT NULL,
    success       BOOLEAN       NOT NULL DEFAULT false,
    created       TIMESTAMP              DEFAULT NULL
);
ALTER TABLE dnys
    ALTER COLUMN created SET DEFAULT NOW();
CREATE INDEX dny_timestamp_idx ON dnys (timestamp);


CREATE TABLE trains
(
    id      SERIAL       NOT NULL PRIMARY KEY,
    hash_id VARCHAR(128) NOT NULL UNIQUE,
    created timestamp DEFAULT NULL
);
ALTER TABLE trains
    ALTER COLUMN created SET DEFAULT NOW();
CREATE INDEX trains_hash_id_idx ON trains (hash_id);


CREATE TABLE train_days
(
    id       SERIAL  NOT NULL PRIMARY KEY,
    train_id INTEGER NOT NULL REFERENCES trains (id),
    date     DATE    NOT NULL,
    created  TIMESTAMP DEFAULT NULL
);
ALTER TABLE train_days
    ALTER COLUMN created SET DEFAULT NOW();
CREATE INDEX train_days_train_id_date_idx ON train_days (train_id, "date");


CREATE TABLE dny_train_infos
(
    id            SERIAL       NOT NULL PRIMARY KEY,
    name          VARCHAR(128) NOT NULL,
    destination   VARCHAR(256) NOT NULL,
    product_class SMALLINT     NOT NULL,
    created       TIMESTAMP DEFAULT NULL,
    UNIQUE (name, destination, product_class)
);
ALTER TABLE dny_train_infos
    ALTER COLUMN created SET DEFAULT NOW();
CREATE INDEX dny_train_infos_search_idx ON dny_train_infos (name, destination, product_class);


CREATE TABLE dny_train_days
(
    id                BIGSERIAL     NOT NULL PRIMARY KEY,
    dny_id            INTEGER       NOT NULL REFERENCES dnys (id),
    train_day_id      INTEGER       NOT NULL REFERENCES train_days (id),
    dny_train_info_id INTEGER       NOT NULL REFERENCES dny_train_infos (id),
    latitude          DECIMAL(9, 6) NOT NULL,
    longitude         DECIMAL(9, 6) NOT NULL,
    direction         SMALLINT      NOT NULL,
    delay             INTEGER,
    created           TIMESTAMP DEFAULT NULL,
    UNIQUE (dny_id, train_day_id)
);
ALTER TABLE dny_train_days
    ALTER COLUMN created SET DEFAULT NOW();
CREATE INDEX dny_train_days_dny_id_idx ON dny_train_days (dny_id);

