// No seu arquivo FireBase.js
import { initializeApp } from 'https://www.gstatic.com/firebasejs/10.12.2/firebase-app.js';
import {
    getAuth,
    signInWithEmailAndPassword,
    signInWithPopup,
    signInWithRedirect,
    getRedirectResult,
    GoogleAuthProvider,
    signOut,
    onAuthStateChanged
} from 'https://www.gstatic.com/firebasejs/10.12.2/firebase-auth.js';

// 🔥 VERIFIQUE SE ESTA CONFIGURAÇÃO ESTÁ CORRETA
const firebaseConfig = {
    apiKey: "AIzaSyBMGBmvLhvar7rpys7NDvzEexbH_TZUEJs",
    authDomain: "controlefinanceiro-35e2a.firebaseapp.com",
    projectId: "controlefinanceiro-35e2a",
    storageBucket: "controlefinanceiro-35e2a.firebasestorage.app",
    messagingSenderId: "521428017393",
    appId: "1:521428017393:web:051e7a3bf6edac00980950",
    measurementId: "G-92FBF0J0MP"
};

// Inicializar Firebase
const app = initializeApp(firebaseConfig);
const auth = getAuth(app);
const googleProvider = new GoogleAuthProvider();

// Configurações adicionais para desenvolvimento local
auth.tenantId = null; // Remove tenant se estiver usando
auth.settings.appVerificationDisabledForTesting = false;

// 🔥 CONFIGURAÇÃO ESPECÍFICA PARA DOMÍNIOS LOCAIS
if (window.location.hostname === 'localhost' ||
    window.location.hostname === '127.0.0.1' ||
    window.location.hostname === '0.0.0.0') {

    // Para desenvolvimento local, você pode precisar de configurações especiais
    console.log('Executando em ambiente de desenvolvimento local');

    // Configuração específica para localhost
    auth.settings.appVerificationDisabledForTesting = true;
}

window.FireBase = {
    signInWithEmail: async (email, password) => {
        try {
            const userCredential = await signInWithEmailAndPassword(auth, email, password);
            return {
                success: true, // 🔥 CORRIGIDO: estava 'Success' no C#
                user: {
                    uid: userCredential.user.uid,
                    email: userCredential.user.email,
                    displayName: userCredential.user.displayName,
                }
            };
        } catch (error) {
            console.error('Erro login email:', error);
            return {
                success: false, // 🔥 CORRIGIDO: estava 'Success' no C#
                errorMessage: error.message // 🔥 CORRIGIDO: estava 'ErrorMessage' no C#
            };
        }
    },

    // 🔥 MÉTODO ORIGINAL (mantido para compatibilidade)
    signInWithGoogle: async () => {
        try {
            // 🔥 VERIFICA SE ESTÁ EM DOMÍNIO AUTORIZADO
            const currentHost = window.location.hostname;
            const authorizedDomains = [
                'localhost',
                '127.0.0.1',
                '0.0.0.0',
                'jt-finance.web.app',
                'jt-finance.firebaseapp.com',
                'controlefinanceiro-35e2a.firebaseapp.com'
            ];

            const isAuthorized = authorizedDomains.some(domain =>
                currentHost.includes(domain) || domain.includes(currentHost)
            );

            if (!isAuthorized) {
                console.warn(`Domínio não autorizado: ${currentHost}. Tentando autenticação...`);
            }

            // Tenta popup primeiro
            const result = await signInWithPopup(auth, googleProvider);

            return {
                success: true, // 🔥 CORRIGIDO
                user: {
                    uid: result.user.uid,
                    email: result.user.email,
                    displayName: result.user.displayName,
                }
            };

        } catch (error) {
            console.error('Erro login Google:', error);

            // Se popup falhar, tenta redirect
            if (error.code === 'auth/popup-blocked' || error.code === 'auth/unauthorized-domain') {
                try {
                    console.log('Tentando login com redirect...');
                    await signInWithRedirect(auth, googleProvider);
                    return {
                        success: true, // 🔥 CORRIGIDO
                        redirect: true
                    };
                } catch (redirectError) {
                    return {
                        success: false, // 🔥 CORRIGIDO
                        errorMessage: `Redirect também falhou: ${redirectError.message}`
                    };
                }
            }

            return {
                success: false, // 🔥 CORRIGIDO
                errorMessage: error.message // 🔥 CORRIGIDO
            };
        }
    },

    // 🔥 NOVOS MÉTODOS ESPECÍFICOS (corrigidos)
    signInWithGooglePopup: async function () {
        try {
            const result = await signInWithPopup(auth, googleProvider);
            return {
                Success: true, // 🔥 Mantido como no C#
                User: {
                    Uid: result.user.uid, // 🔥 CORRIGIDO: estava 'uid'
                    Email: result.user.email, // 🔥 CORRIGIDO: estava 'email'
                    DisplayName: result.user.displayName, // 🔥 CORRIGIDO: estava 'displayName'
                    PhotoURL: result.user.photoURL // 🔥 CORRIGIDO: estava 'photoURL'
                }
            };
        } catch (error) {
            console.error('Google popup sign-in error:', error);
            return {
                Success: false, // 🔥 Mantido como no C#
                ErrorMessage: error.message // 🔥 Mantido como no C#
            };
        }
    },

    // Login com Google via Redirect (fallback)
    signInWithGoogleRedirect: async function () {
        try {
            await signInWithRedirect(auth, googleProvider);
            return {
                Success: true, // 🔥 Mantido como no C#
                Redirect: true
            };
        } catch (error) {
            console.error('Google redirect sign-in error:', error);
            return {
                Success: false, // 🔥 Mantido como no C#
                ErrorMessage: error.message // 🔥 Mantido como no C#
            };
        }
    },

    getGoogleRedirectResult: async () => {
        try {
            const result = await getRedirectResult(auth);
            if (result && result.user) {
                return {
                    Success: true, // 🔥 CORRIGIDO: estava 'success'
                    User: { // 🔥 CORRIGIDO: estava 'user'
                        Uid: result.user.uid, // 🔥 CORRIGIDO: estava 'uid'
                        Email: result.user.email, // 🔥 CORRIGIDO: estava 'email'
                        DisplayName: result.user.displayName, // 🔥 CORRIGIDO: estava 'displayName'
                    }
                };
            }

            // Verifica usuário atual
            const currentUser = auth.currentUser;
            if (currentUser) {
                return {
                    Success: true, // 🔥 CORRIGIDO
                    User: { // 🔥 CORRIGIDO
                        Uid: currentUser.uid, // 🔥 CORRIGIDO
                        Email: currentUser.email, // 🔥 CORRIGIDO
                        DisplayName: currentUser.displayName, // 🔥 CORRIGIDO
                    }
                };
            }

            return {
                Success: false, // 🔥 CORRIGIDO
                ErrorMessage: "Nenhum resultado de redirect encontrado" // 🔥 CORRIGIDO
            };

        } catch (error) {
            console.error('Erro redirect result:', error);
            return {
                Success: false, // 🔥 CORRIGIDO
                ErrorMessage: error.message // 🔥 CORRIGIDO
            };
        }
    },

    signOut: async () => {
        try {
            await signOut(auth);
            return { Success: true }; // 🔥 ADICIONADO retorno compatível
        } catch (error) {
            console.error('Erro logout:', error);
            return {
                Success: false, // 🔥 CORRIGIDO
                ErrorMessage: error.message // 🔥 CORRIGIDO
            };
        }
    },

    onAuthStateChanged: (dotNetHelper) => {
        return onAuthStateChanged(auth, (user) => {
            if (user) {
                dotNetHelper.invokeMethodAsync('OnAuthStateChanged', {
                    Uid: user.uid, // 🔥 CORRIGIDO: estava 'uid'
                    Email: user.email, // 🔥 CORRIGIDO: estava 'email'
                    DisplayName: user.displayName // 🔥 CORRIGIDO: estava 'displayName'
                });
            } else {
                dotNetHelper.invokeMethodAsync('OnAuthStateChanged', null);
            }
        });
    },

    // 🔥 NOVO: Método para verificar configuração
    checkConfiguration: () => {
        const authorizedDomains = [
            'localhost',
            '127.0.0.1',
            '0.0.0.0',
            'jt-finance.web.app',
            'jt-finance.firebaseapp.com',
            'controlefinanceiro-35e2a.firebaseapp.com'
        ];

        const currentDomain = window.location.hostname;
        const isAuthorized = authorizedDomains.some(domain =>
            currentDomain.includes(domain) || domain.includes(currentDomain)
        );

        const config = {
            domain: currentDomain,
            authorized: isAuthorized,
            firebaseConfig: firebaseConfig ? 'Configurado' : 'Não configurado',
            authDomain: firebaseConfig.authDomain
        };

        console.log('Firebase Configuration Check:', config);
        return config;
    },

    // 🔥 NOVO: Método para debug
    getCurrentUser: () => {
        const user = auth.currentUser;
        if (user) {
            return {
                Uid: user.uid,
                Email: user.email,
                DisplayName: user.displayName,
                EmailVerified: user.emailVerified
            };
        }
        return null;
    }
};