import {
  AppBar,
  Box,
  Button,
  Container,
  Divider,
  Stack,
  Toolbar,
  Typography
} from "@mui/material";
import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { useAuth } from "../../features/auth/AuthContext";

const navItems = [
  { label: "О себе", to: "/about" },
  { label: "Проекты с ИИ", to: "/ai-projects" },
  { label: "Другие проекты", to: "/other-projects" }
];

export function AdminLayout() {
  const auth = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    auth.logout();
    navigate("/login", { replace: true });
  };

  return (
    <Box sx={{ minHeight: "100vh" }}>
      <AppBar color="inherit" elevation={0} position="sticky" sx={{ borderBottom: 1, borderColor: "divider" }}>
        <Container maxWidth="lg">
          <Toolbar disableGutters sx={{ gap: 2, minHeight: 72 }}>
            <Box sx={{ minWidth: 0 }}>
              <Typography component="div" sx={{ fontWeight: 700 }} variant="h6">
                Portfolio Admin
              </Typography>
              <Typography color="text.secondary" noWrap variant="caption">
                {auth.user?.login}
              </Typography>
            </Box>

            <Stack
              direction="row"
              spacing={1}
              sx={{
                display: { xs: "none", md: "flex" },
                flex: 1,
                justifyContent: "center"
              }}
            >
              {navItems.map((item) => (
                <Button
                  key={item.to}
                  component={NavLink}
                  sx={{
                    "&.active": {
                      backgroundColor: "action.selected",
                      color: "primary.main"
                    }
                  }}
                  to={item.to}
                >
                  {item.label}
                </Button>
              ))}
            </Stack>

            <Box sx={{ flex: { xs: 1, md: 0 } }} />

            <Button color="inherit" onClick={handleLogout}>
              Выйти
            </Button>
          </Toolbar>

          <Stack
            direction="row"
            spacing={1}
            sx={{
              display: { xs: "flex", md: "none" },
              overflowX: "auto",
              pb: 1.5
            }}
          >
            {navItems.map((item) => (
              <Button
                key={item.to}
                component={NavLink}
                size="small"
                sx={{
                  flexShrink: 0,
                  "&.active": {
                    backgroundColor: "action.selected",
                    color: "primary.main"
                  }
                }}
                to={item.to}
              >
                {item.label}
              </Button>
            ))}
          </Stack>
        </Container>
      </AppBar>

      <Container component="main" maxWidth="lg" sx={{ py: { xs: 3, md: 5 } }}>
        <Outlet />
      </Container>

      <Divider />
    </Box>
  );
}
