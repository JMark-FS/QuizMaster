import jwtDecode from "jwt-decode";
import { AuthOptions, ISODateString, User } from "next-auth";
import NextAuth from "next-auth/next";
import CredentialsProvider from "next-auth/providers/credentials";
import { Session } from "next-auth";
import { JWT } from "next-auth/jwt";

interface DataToken {
    token: string;
    nbf: ISODateString;
    exp: ISODateString;
    iat: ISODateString;
}
const authOptions: AuthOptions = {
    providers: [
        CredentialsProvider({
            // The name to display on the sign in form (e.g. 'Sign in with...')
            name: "credentials",
            // The credentials is used to generate a suitable form on the sign in page.
            // You can specify whatever fields you are expecting to be submitted.
            // e.g. domain, username, password, 2FA token, etc.
            // You can pass any HTML attribute to the <input> tag through the object.
            credentials: {
                jwt: {},
            },
            async authorize(credentials, req) {
                try {
                    // Guard of null jwt token
                    console.log("Method invoked ");
                    console.log(credentials);
                    if (!credentials?.jwt) {
                        return null;
                    }

                    // Decode jwt token
                    const decode: DataToken = jwtDecode(credentials?.jwt);
                    const parse = JSON.parse(decode.token);
                    const parsed = parse.UserData;
                    console.log("AUTH Authorize line 39: ", parse, parsed);
                    // Assign parsed user to user
                    const user = {
                        id: parsed.id,
                        name: `${parsed.FirstName} ${parsed.LastName}`,
                        email: parsed.Email,
                        username: parsed.UserName,
                        role: parse.Roles[0].Name,
                    };
                    console.log("Returning user", user);
                    return user as any;
                } catch (error) {
                    console.log(error);
                }
            },
        }),
    ],
    pages: {
        signIn: "/auth/login",
        signOut: "/",
    },

    secret:
        process.env.NEXTAUTH_SECRET ??
        process.env.SECRET ??
        "04e9d3fbe3c8fbfb7e5f89892751f8c5",
    callbacks: {
        jwt: async ({ token, user }: { token: JWT; user: User }) => {
            user &&
                (token.user = {
                    ...user,
                    username: "",
                    role: "",
                    name: user.name!,
                    email: user.email!,
                });
            console.log("AUTH callback JWT: line 74: ", token, user);
            return token;
        },
        signIn: async ({ user }: { user: User }) => {
            console.log("AUTH callback signIn: line 79: ", user);
            if (user.name) return true;
            return false;
        },
        session: ({ session, token }: { session: Session; token: JWT }) => {
            session.user = token.user;

            return session;
        },
    },
};

const handler = NextAuth(authOptions as any);

export { handler as GET, handler as POST };
