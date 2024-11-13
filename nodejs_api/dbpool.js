var mysql = require('mysql');

const pool = mysql.createPool({
    connectionLimit: 100,
    host: 'localhost',
    port: 3306,
    user: 'root',
    password: '',
    database: 'kira_db',
    debug: false
});

pool.getConnection((err, connection) => {
    if(err) throw err;
    console.log('Database connected successfully');
    connection.release();
});

module.exports = pool;
