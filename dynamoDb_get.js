// Import the AWS SDK
const AWS = require('aws-sdk');

// Create a new DynamoDB client
const dynamoDb = new AWS.DynamoDB.DocumentClient();

exports.handler = async (event) => {
    // Define the parameters for the DynamoDB query
    const params = {
        TableName: 'YourTableName',
        Key: {
            'PrimaryKey': event.key
        }
    };

    try {
        // Perform a get operation on DynamoDB
        const data = await dynamoDb.get(params).promise();
        
        // Return the retrieved item
        return {
            statusCode: 200,
            body: JSON.stringify(data.Item)
        };
    } catch (error) {
        // Handle any errors
        return {
            statusCode: 500,
            body: JSON.stringify({ error: error.message })
        };
    }
};
