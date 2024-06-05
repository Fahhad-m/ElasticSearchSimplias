import React, { useState } from 'react';
import axios from 'axios';
import '../index.css';

const ProductForm = ({ onProductAdded }) => {
    const [id, setId] = useState('');
    const [name, setName] = useState('');
    const [description, setDescription] = useState('');
    const [price, setPrice] = useState('');

    const handleSubmit = async (event) => {
        event.preventDefault();
        const newProduct = { id, name, description, price: parseFloat(price) };
        try {
            const response = await axios.post('https://localhost:44373/api/Products/CreateProduct', newProduct);
            onProductAdded(response.data);
            setId('');
            setName('');
            setDescription('');
            setPrice('');
        } catch (error) {
            console.error('Error creating product:', error);
            // Handle error: Display an error message to the user or take appropriate action.
        }
    };

    return (
        <form className="form-container" onSubmit={handleSubmit}>
            <div className="form-field">
                <label htmlFor="id">ID</label>
                <input
                    type="text"
                    id="id"
                    value={id}
                    onChange={(e) => setId(e.target.value)}
                    required
                />
            </div>
            <div className="form-field">
                <label htmlFor="name">Product Name</label>
                <input
                    type="text"
                    id="name"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    required
                />
            </div>
            <div className="form-field">
                <label htmlFor="description">Description</label>
                <textarea
                    id="description"
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    required
                />
            </div>
            <div className="form-field">
                <label htmlFor="price">Price</label>
                <input
                    type="number"
                    id="price"
                    value={price}
                    onChange={(e) => setPrice(e.target.value)}
                    step="0.01"
                    required
                />
            </div>
            <div className="form-field">
                <button type="submit">Add Product</button>
            </div>
        </form>
    );
};

export default ProductForm;
