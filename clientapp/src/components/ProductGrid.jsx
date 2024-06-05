import React, { useEffect, useState } from 'react';
import axios from 'axios';
import '../index.css';

const ProductGrid = () => {
    const [products, setProducts] = useState([]);

    useEffect(() => {
        axios.get('https://localhost:44373/api/Products/GetAllProducts')
            .then(response => setProducts(response.data))
            .catch(error => console.error('Error fetching products:', error));
    }, []);

    return (
        <div className="grid-container">
            {products.map(product => (
                <div key={product.id} className="grid-item">
                    <h3>{product.name}</h3>
                    <p>{product.description}</p>
                    <p>${product.price.toFixed(2)}</p>
                </div>
            ))}
        </div>
    );
};

export default ProductGrid;
