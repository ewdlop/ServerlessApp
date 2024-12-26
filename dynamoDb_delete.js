exports.deleteItem = async (event) => {
    const params = {
        TableName: 'YourTableName',
        Key: {
            id: event.pathParameters.id
        }
    };

    try {
        await dynamoDb.delete(params).promise();
        return {
            statusCode: 200,
            body: JSON.stringify({ message: 'Item deleted successfully' })
        };
    } catch (error) {
        return {
            statusCode: 500,
            body: JSON.stringify({ error: error.message })
        };
    }
};
