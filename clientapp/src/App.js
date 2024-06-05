import React, { useState } from 'react';
import ProductGrid from './components/ProductGrid';
import ProductForm from './components/ProductForm';


const App = () => {
    const [products, setProducts] = useState([]);

    const handleProductAdded = (newProduct) => {
        setProducts([...products, newProduct]);
    };

  

    return (
        <div>
            <nav>
                <ul>
                    <li><a href="/">Home</a></li>
                    <li><a href="/swagger/index.html" target="_blank" rel="noopener noreferrer">API Documentation</a></li>
                </ul>
            </nav>
            <h1>Product Management</h1>
            <ProductForm onProductAdded={handleProductAdded} />
            <ProductGrid />
        </div>
    );
};

export default App;
