// 1. IMPORTS: We must use the full URL (CDN) for Blazor WebAssembly
import { initializeApp } from "https://www.gstatic.com/firebasejs/10.8.0/firebase-app.js";
import { getFirestore, collection, addDoc, getDocs } from "https://www.gstatic.com/firebasejs/10.8.0/firebase-firestore.js";
import { getAuth, GoogleAuthProvider, signInWithPopup } from "https://www.gstatic.com/firebasejs/10.8.0/firebase-auth.js";

// 2. CONFIGURATION: Your specific keys
const firebaseConfig = {
    apiKey: "AIzaSyDar5ZlJA9iEuGjAw-vT2U2Hlbl0NT4AAQ",
    authDomain: "storhaugenwebsite.firebaseapp.com",
    projectId: "storhaugenwebsite",
    storageBucket: "storhaugenwebsite.firebasestorage.app",
    messagingSenderId: "55384801378",
    appId: "1:55384801378:web:a85286c573dc614f46513b",
    measurementId: "G-ZSQ7TJKCN7"
};

// 3. INITIALIZATION: Initialize the app only once
const app = initializeApp(firebaseConfig);
const db = getFirestore(app);
const auth = getAuth(app);

// 4. EXPORTED FUNCTIONS: These are called by your C# Blazor code

// Login Function
export async function loginWithGoogle() {
    const provider = new GoogleAuthProvider();
    try {
        const result = await signInWithPopup(auth, provider);
        return result.user.email;
    } catch (error) {
        console.error("Login Error:", error);
        return null;
    }
}

// Add Item Function
export async function addFoodItem(name, rating) {
    try {
        await addDoc(collection(db, "foods"), {
            name: name,
            rating: parseInt(rating),
            date: new Date()
        });
        return true;
    } catch (e) {
        console.error("Error adding document: ", e);
        return false;
    }
}

// Get Items Function
export async function getFoodItems() {
    try {
        const querySnapshot = await getDocs(collection(db, "foods"));
        let foods = [];
        querySnapshot.forEach((doc) => {
            foods.push({ id: doc.id, ...doc.data() });
        });
        return foods;
    } catch (e) {
        console.error("Error getting documents: ", e);
        return [];
    }
}