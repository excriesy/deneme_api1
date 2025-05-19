import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { Layout } from 'antd';
import { AuthProvider } from './contexts/AuthContext';
import Navbar from './components/Navbar';
import Login from './pages/Login';
import Register from './pages/Register';
import Dashboard from './pages/Dashboard';
import Files from './pages/Files';
import PrivateRoute from './components/PrivateRoute';

const App: React.FC = () => {
    return (
        <AuthProvider>
            <Router>
                <Layout>
                    <Navbar />
                    <Routes>
                        <Route path="/login" element={<Login />} />
                        <Route path="/register" element={<Register />} />
                        <Route
                            path="/dashboard"
                            element={
                                <PrivateRoute>
                                    <Dashboard />
                                </PrivateRoute>
                            }
                        />
                        <Route
                            path="/files"
                            element={
                                <PrivateRoute>
                                    <Files />
                                </PrivateRoute>
                            }
                        />
                        <Route path="/" element={<Navigate to="/dashboard" replace />} />
                    </Routes>
                </Layout>
            </Router>
        </AuthProvider>
    );
};

export default App;
