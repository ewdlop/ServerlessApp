const AWS = require('aws-sdk');
const dynamoDb = new AWS.DynamoDB.DocumentClient();

exports.createItem = async (event) => {
    const requestBody = JSON.parse(event.body);
    const params = {
        TableName: 'YourTableName',
        Item: {
            id: requestBody.id,
            data: requestBody.data
        }
    };

    try {
        await dynamoDb.put(params).promise();
        return {
            statusCode: 201,
            body: JSON.stringify({ message: 'Item created successfully' })
        };
    } catch (error) {
        return {
            statusCode: 500,
            body: JSON.stringify({ error: error.message })
        };
    }
};
