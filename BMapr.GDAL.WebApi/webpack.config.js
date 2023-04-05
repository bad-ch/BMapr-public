const path = require('path');

module.exports = {
    entry: './scripts/app.ts',
    mode: "development",
    devtool: 'inline-source-map',
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                use: 'ts-loader',
                exclude: /node_modules/,
            },
        ],
    },
    resolve: {
        extensions: ['.tsx', '.ts', '.js'],
    },
    output: {
        library: {
            name: 'MYAPP',
            type: 'var'
        },
        filename: 'app-dist.js',
        path: path.resolve(__dirname, 'wwwroot/js'),
    }
};