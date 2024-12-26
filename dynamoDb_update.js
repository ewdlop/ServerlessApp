exports.updateItem = async (event) => {
    const requestBody = JSON.parse(event.body);
    const params = {
        TableName: 'YourTableName',
        Key: {
            id: event.pathParameters.id
        },
        UpdateExpression: 'set #data = :data',
        ExpressionAttributeNames: {
            '#data': 'data'
        },
        ExpressionAttributeValues: {
            ':data': requestBody.data
        },
        ReturnValues: 'UPDATED_NEW'
    };

    try {
        const data = await dynamoDb.update(params).promise();
        return {
            statusCode: 200,
            body: JSON.stringify({ message: 'Item updated successfully', data: data.Attributes })
        };
    } catch (error) {
        return {
            statusCode: 500,
            body: JSON.stringify({ error: error.message })
        };
    }
};
